using System.Collections.Generic;
using System.Linq;
using SpaceSurvivors.Combat;
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
        [SerializeField] private GameObject _player;
        [SerializeField] private StatSheet _stats;
        [SerializeField] private WeaponController _weapons;
        [SerializeField] private List<UpgradeData> _catalogue = new();

        private readonly Dictionary<UpgradeData, int> _timesTaken = new();
        private readonly HashSet<WeaponData> _evolved = new();

        private void Awake()
        {
            if (_player == null) _player = gameObject;
            if (_stats == null) _stats = _player.GetComponent<StatSheet>();
            if (_weapons == null) _weapons = _player.GetComponent<WeaponController>();
        }

        // ---------------------------------------------------------------- drafting

        /// <summary>
        /// Build the level-up choices: any ready weapon evolution first (guaranteed slot),
        /// then weighted, non-duplicate, still-available upgrades.
        /// </summary>
        public IReadOnlyList<UpgradeOffer> Roll(int count)
        {
            var offers = new List<UpgradeOffer>(count);

            foreach (var evo in ReadyEvolutions())
            {
                offers.Add(UpgradeOffer.Evolution(evo.from, evo.into));
                if (offers.Count >= count) return offers;
            }

            var available = _catalogue
                .Where(u => u != null && (u.maxStacks == 0 || TakenCount(u) < u.maxStacks))
                .Where(u => u.special != UpgradeData.SpecialEffect.GrantWeapon
                            || (u.weaponToGrant != null && !_weapons.HasWeapon(u.weaponToGrant)))
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

        // ---------------------------------------------------------------- applying

        public void Apply(in UpgradeOffer offer)
        {
            if (offer.IsEvolution)
            {
                _weapons.EvolveWeapon(offer.EvolveFrom, offer.EvolveInto);
                _evolved.Add(offer.EvolveFrom);
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

    /// <summary>A single card on the level-up screen — a normal upgrade or a weapon evolution.</summary>
    public readonly struct UpgradeOffer
    {
        public readonly UpgradeData Upgrade;
        public readonly WeaponData EvolveFrom;
        public readonly WeaponData EvolveInto;
        public bool IsEvolution => EvolveInto != null;

        private UpgradeOffer(UpgradeData u, WeaponData from, WeaponData into)
        {
            Upgrade = u; EvolveFrom = from; EvolveInto = into;
        }

        public static UpgradeOffer Normal(UpgradeData u) => new(u, null, null);
        public static UpgradeOffer Evolution(WeaponData from, WeaponData into) => new(null, from, into);

        public string Title => IsEvolution ? $"EVOLVE: {EvolveInto.displayName}" : Upgrade.title;
        public string Description => IsEvolution
            ? $"{EvolveFrom.displayName} reaches its final form"
            : Upgrade.description;
    }
}
