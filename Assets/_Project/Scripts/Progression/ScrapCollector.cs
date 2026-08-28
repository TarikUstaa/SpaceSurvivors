using System;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// The player's "hand" for pickups. Owns the base magnet/collect radii and forwards
    /// collected value to the <see cref="LevelSystem"/>. Radii and XP gain run through the
    /// <see cref="StatSheet"/> so upgrades stay data-driven (AI_Guidelines §3). Pickups query
    /// this for the current radii and call <see cref="Absorb"/> when they reach the ship.
    /// </summary>
    [RequireComponent(typeof(LevelSystem))]
    [DisallowMultipleComponent]
    public class ScrapCollector : MonoBehaviour
    {
        [Header("Base radii (world units)")]
        [SerializeField, Min(0.1f)] private float _collectRadius = 0.6f;
        [SerializeField, Min(0.1f)] private float _magnetRadius = 2.5f;

        [SerializeField] private StatSheet _stats;

        private LevelSystem _levelSystem;

        public int TotalScrap { get; private set; }

        public float CollectRadius => Modify(_collectRadius);
        public float MagnetRadius => Modify(_magnetRadius);

        public event Action<int> ScrapCollected;

        private void Awake()
        {
            _levelSystem = GetComponent<LevelSystem>();
            if (_stats == null) _stats = GetComponent<StatSheet>();
        }

        private float Modify(float radius)
            => _stats != null ? _stats.Modify(StatId.PickupRadius, radius) : radius;

        /// <summary>
        /// Called by a pickup once it reaches the ship. <paramref name="guaranteedLevels"/> &gt; 0
        /// (boss orbs) forces that many extra full level-ups on top of the XP value.
        /// </summary>
        public void Absorb(int scrapValue, int xpValue, int guaranteedLevels = 0)
        {
            TotalScrap += scrapValue;

            int xp = _stats != null
                ? Mathf.RoundToInt(_stats.Modify(StatId.XpGain, xpValue))
                : xpValue;

            _levelSystem.AddXp(xp);
            if (guaranteedLevels > 0) _levelSystem.GrantLevels(guaranteedLevels);
            ScrapCollected?.Invoke(scrapValue);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, MagnetRadius);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, CollectRadius);
        }
#endif
    }
}
