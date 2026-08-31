using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Seeds the player's <see cref="StatSheet"/> with the permanent upgrades bought in the
    /// shop (M14a), once, at run start. Runs early so <see cref="SpaceSurvivors.Combat.HealthComponent"/>
    /// and friends see the boosted numbers when they initialise (they also listen to
    /// <see cref="StatSheet.Changed"/>, so order isn't fragile).
    /// </summary>
    [RequireComponent(typeof(StatSheet))]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class MetaUpgradeApplier : MonoBehaviour
    {
        [SerializeField] private StatSheet _stats;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<StatSheet>();
            _stats.AddModifiers(MetaProgressionService.BuildStartingModifiers());
        }
    }
}
