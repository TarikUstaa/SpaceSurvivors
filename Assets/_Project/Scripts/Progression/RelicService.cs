using System.Collections.Generic;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Tracks which relics this run has picked up and applies their bonuses once, permanently —
    /// unlike <see cref="WeaponSynergyTracker"/>'s synergies, a relic never gets removed once
    /// granted, so this only ever adds. Lives on the Player; a fresh run means a fresh Player
    /// instance (the scene reloads on Replay), so <see cref="_owned"/> resetting via a normal
    /// field is enough — no explicit "run started" signal needed, same reasoning as
    /// <see cref="SpaceSurvivors.Combat.SecondWind"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class RelicService : MonoBehaviour
    {
        [Tooltip("Optional. Empty = auto-resolve a StatSheet on this GameObject.")]
        [SerializeField] private StatSheet _stats;

        private readonly HashSet<RelicData> _owned = new();

        /// <summary>Raised the moment a relic is granted — for a toast/notification.</summary>
        public event System.Action<RelicData> RelicGranted;

        public IReadOnlyCollection<RelicData> Owned => _owned;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<StatSheet>();
        }

        public bool Owns(RelicData relic) => relic != null && _owned.Contains(relic);

        /// <summary>Grants a relic's bonuses if this run doesn't already have it. Returns false
        /// if it was already owned (nothing applied twice) or the relic is null.</summary>
        public bool TryGrant(RelicData relic)
        {
            if (relic == null || _stats == null || !_owned.Add(relic)) return false;

            _stats.AddModifiers(relic.bonuses);
            RelicGranted?.Invoke(relic);
            return true;
        }
    }
}
