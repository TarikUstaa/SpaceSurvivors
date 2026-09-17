using System.Collections.Generic;
using System.Linq;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Owns the upgrade catalogue and the level-up draft. An ordinary upgrade is just a bag
    /// of <see cref="StatModifier"/>s poured into the player's <see cref="StatSheet"/> — the
    /// whole "modifier pipeline" the guidelines call for, with zero code per upgrade (§3).
    /// Special cases: granting a weapon, and offering a weapon <b>evolution</b> once its
    /// catalyst passive is maxed.
    /// </summary>
    [DisallowMultipleComponent]
    public class UpgradeService : MonoBehaviour
    {
        private const string CurseCatalogueResource = "CurseCatalogue";

        [SerializeField] private GameObject _player;
        [SerializeField] private StatSheet _stats;
        [SerializeField] private WeaponController _weapons;
        [SerializeField] private List<UpgradeData> _catalogue = new();

        [Header("Curses")]
        [Tooltip("Chance that one of the level-up slots is a curse instead of an upgrade.")]
        [SerializeField, Range(0f, 1f)] private float _curseChance = 0.25f;
        [Tooltip("No curses before this level — an early one is a bargain with nothing to bargain with.")]
        [SerializeField, Min(1)] private int _curseMinLevel = 4;

        private readonly Dictionary<UpgradeData, int> _timesTaken = new();
        private readonly HashSet<WeaponData> _evolved = new();
        private readonly HashSet<CurseData> _cursesTaken = new();
        private readonly List<CurseData> _curseCandidates = new();

        private static CurseCatalogue _curses;
        private LevelSystem _levelSystem;

        /// <summary>Raised when a curse is accepted, so the HUD can announce it.</summary>
        public event System.Action<CurseData> CurseTaken;

        public IReadOnlyCollection<CurseData> TakenCurses => _cursesTaken;

        private void Awake()
        {
            if (_player == null) _player = gameObject;
            if (_stats == null) _stats = _player.GetComponent<StatSheet>();
            if (_weapons == null) _weapons = _player.GetComponent<WeaponController>();
            if (_levelSystem == null) _levelSystem = FindAnyObjectByType<LevelSystem>();
            if (_curses == null) _curses = Resources.Load<CurseCatalogue>(CurseCatalogueResource);

            // The backoffice may override both for this run (RemoteConfig). Copied here, once, so a
            // change never lands between two level-ups of the same run.
            _curseChance = RemoteConfig.Float(RemoteConfig.Keys.CurseChance, _curseChance);
            _curseMinLevel = RemoteConfig.Int(RemoteConfig.Keys.CurseMinLevel, _curseMinLevel);
        }

        // ---------------------------------------------------------------- drafting

        /// <summary>
        /// Build the level-up choices: any ready weapon evolution first (guaranteed slot),
        /// then weighted, non-duplicate, still-available upgrades — with one slot sometimes
        /// given over to a curse.
        ///
        /// <para>A curse takes a slot rather than being added as an extra card, which is what
        /// makes refusing it cost something: the alternative, a fourth card you can simply
        /// ignore, is not a decision. It is still opt-in in the way that matters — the other
        /// cards are ordinary upgrades, and nothing forces the bargain.</para>
        /// </summary>
        public IReadOnlyList<UpgradeOffer> Roll(int count)
        {
            var offers = new List<UpgradeOffer>(count);

            foreach (var evo in ReadyEvolutions())
            {
                offers.Add(UpgradeOffer.Evolution(evo.from, evo.into));
                if (offers.Count >= count) return offers;
            }

            // Drafted before the upgrades so it competes for a slot rather than being appended
            // to a list that is already full.
            CurseData curse = RollCurse();
            if (curse != null) offers.Add(UpgradeOffer.Curse(curse));

            var available = _catalogue
                .Where(u => u != null && (u.maxStacks == 0 || TakenCount(u) < u.maxStacks))
                .Where(u => u.special != UpgradeData.SpecialEffect.GrantWeapon
                            || (u.weaponToGrant != null && !_weapons.HasWeapon(u.weaponToGrant)
                                && !_weapons.IsFull))
                .ToList();

            while (offers.Count < count && available.Count > 0)
            {
                float total = available.Sum(u => Mathf.Max(0f, u.weight));
                if (total <= 0f) break;

                float r = Random.value * total;
                UpgradeData chosen = available[^1];
                foreach (var u in available)
                {
                    r -= Mathf.Max(0f, u.weight);
                    if (r <= 0f) { chosen = u; break; }
                }

                offers.Add(UpgradeOffer.Normal(chosen));
                available.Remove(chosen);
            }
            return offers;
        }

        /// <summary>
        /// Pick a curse to offer, or null. Null is the common answer: most level-ups should be
        /// an ordinary choice, and a curse every time would stop reading as a gamble.
        /// </summary>
        private CurseData RollCurse()
        {
            if (_curses == null || _stats == null) return null;
            if (_levelSystem != null && _levelSystem.CurrentLevel < _curseMinLevel) return null;
            if (Random.value >= _curseChance) return null;

            _curseCandidates.Clear();
            foreach (var curse in _curses.curses)
                if (curse != null && !_cursesTaken.Contains(curse)) _curseCandidates.Add(curse);

            if (_curseCandidates.Count == 0) return null;

            float total = 0f;
            foreach (var curse in _curseCandidates) total += Mathf.Max(0f, curse.weight);
            if (total <= 0f) return null;

            float r = Random.value * total;
            foreach (var curse in _curseCandidates)
            {
                r -= Mathf.Max(0f, curse.weight);
                if (r <= 0f) return curse;
            }
            return _curseCandidates[^1];
        }

        // ---------------------------------------------------------------- applying

        public void Apply(in UpgradeOffer offer)
        {
            if (offer.IsEvolution)
            {
                _weapons.EvolveWeapon(offer.EvolveFrom, offer.EvolveInto);
                _evolved.Add(offer.EvolveFrom);
                return;
            }

            if (offer.IsCurse)
            {
                CurseData curse = offer.CurseData;
                if (curse == null || !_cursesTaken.Add(curse)) return;

                // Both halves go through the same pipeline; only the sign differs.
                if (_stats != null)
                {
                    _stats.AddModifiers(curse.boons);
                    _stats.AddModifiers(curse.costs);
                }
                CurseTaken?.Invoke(curse);
                return;
            }

            UpgradeData u = offer.Upgrade;
            if (u == null) return;

            _timesTaken[u] = TakenCount(u) + 1;

            if (u.modifiers is { Length: > 0 } && _stats != null)
                _stats.AddModifiers(u.modifiers);

            if (u.special == UpgradeData.SpecialEffect.GrantWeapon && u.weaponToGrant != null)
                _weapons.AddWeapon(u.weaponToGrant);
        }

        public int TakenCount(UpgradeData u) => _timesTaken.TryGetValue(u, out int n) ? n : 0;

        // ---------------------------------------------------------------- evolutions

        private IEnumerable<(WeaponData from, WeaponData into)> ReadyEvolutions()
        {
            foreach (var w in _weapons.Weapons)
            {
                if (w == null || w.evolvesInto == null || _evolved.Contains(w)) continue;
                if (w.evolutionCatalyst == null) continue;
                if (TakenCount(w.evolutionCatalyst) >= Mathf.Max(1, w.evolutionCatalyst.maxStacks))
                    yield return (w, w.evolvesInto);
            }
        }
    }

    /// <summary>
    /// A single card on the level-up screen — a normal upgrade, a weapon evolution, or a curse.
    /// </summary>
    public readonly struct UpgradeOffer
    {
        public readonly UpgradeData Upgrade;
        public readonly WeaponData EvolveFrom;
        public readonly WeaponData EvolveInto;
        public readonly CurseData CurseData;

        public bool IsEvolution => EvolveInto != null;
        public bool IsCurse => CurseData != null;

        private UpgradeOffer(UpgradeData u, WeaponData from, WeaponData into, CurseData curse)
        {
            Upgrade = u; EvolveFrom = from; EvolveInto = into; CurseData = curse;
        }

        public static UpgradeOffer Normal(UpgradeData u) => new(u, null, null, null);
        public static UpgradeOffer Evolution(WeaponData from, WeaponData into) => new(null, from, into, null);
        public static UpgradeOffer Curse(CurseData curse) => new(null, null, null, curse);

        public string Title => IsEvolution
            ? $"EVOLVE: {EvolveInto.displayName}"
            : IsCurse ? CurseData.displayName : Upgrade.title;

        /// <summary>
        /// A curse describes itself in two coloured lines, because the decision is the
        /// comparison: what it gives above what it takes.
        /// </summary>
        public string Description
        {
            get
            {
                if (IsEvolution) return $"{EvolveFrom.displayName} reaches its final form";
                if (!IsCurse) return Upgrade.description;
                return $"<color=#8FE39A>{CurseData.boonText}</color>\n" +
                       $"<color=#F08A8A>{CurseData.costText}</color>";
            }
        }
    }
}
