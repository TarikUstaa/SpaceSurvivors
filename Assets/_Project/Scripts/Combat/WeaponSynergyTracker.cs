using System.Collections.Generic;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Watches <see cref="WeaponController.LoadoutChanged"/> and keeps the player's
    /// <see cref="StatSheet"/> in sync with whichever <see cref="WeaponSynergyData"/> pairs are
    /// currently satisfied — adds a synergy's bonuses the moment both its weapons are equipped,
    /// removes them the moment either one is gone (evolved away, most likely; nothing currently
    /// un-equips a weapon otherwise). <see cref="StatSheet"/>'s modifiers are a plain
    /// accumulator with no dedup (AI_Guidelines — see its own doc comment), so this component
    /// is what keeps a synergy from being added twice or leaking after its weapon is gone;
    /// <see cref="_active"/> is the source of truth for which synergies currently hold a live
    /// modifier.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponSynergyTracker : MonoBehaviour
    {
        private const string CatalogueResource = "WeaponSynergyCatalogue";

        [Tooltip("Optional. Empty = auto-resolve a WeaponController on this GameObject.")]
        [SerializeField] private WeaponController _weapons;
        [Tooltip("Optional. Empty = auto-resolve a StatSheet on this GameObject.")]
        [SerializeField] private StatSheet _stats;

        private static WeaponSynergyCatalogue _catalogue;
        private readonly HashSet<WeaponSynergyData> _active = new();

        /// <summary>Raised when a synergy turns on or off — for a toast/notification, if one
        /// ever gets wired up. Not required for the bonus itself to apply.</summary>
        public event System.Action<WeaponSynergyData, bool> SynergyToggled;

        private void Awake()
        {
            if (_weapons == null) _weapons = GetComponent<WeaponController>();
            if (_stats == null) _stats = GetComponent<StatSheet>();
            if (_catalogue == null) _catalogue = Resources.Load<WeaponSynergyCatalogue>(CatalogueResource);
        }

        private void OnEnable()
        {
            if (_weapons != null) _weapons.LoadoutChanged += Refresh;
        }

        private void OnDisable()
        {
            if (_weapons != null) _weapons.LoadoutChanged -= Refresh;
        }

        private void Start() => Refresh();

        private void Refresh()
        {
            if (_catalogue == null || _weapons == null || _stats == null) return;

            foreach (var synergy in _catalogue.synergies)
            {
                if (synergy == null) continue;

                bool satisfied = synergy.weaponA != null && synergy.weaponB != null
                    && _weapons.HasWeapon(synergy.weaponA) && _weapons.HasWeapon(synergy.weaponB);
                bool wasActive = _active.Contains(synergy);

                if (satisfied && !wasActive)
                {
                    _stats.AddModifiers(synergy.bonuses);
                    _active.Add(synergy);
                    SynergyToggled?.Invoke(synergy, true);
                }
                else if (!satisfied && wasActive)
                {
                    foreach (var mod in synergy.bonuses) _stats.RemoveModifier(mod);
                    _active.Remove(synergy);
                    SynergyToggled?.Invoke(synergy, false);
                }
            }
        }
    }
}
