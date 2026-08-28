using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Stats for one enemy archetype. The <see cref="SpaceSurvivors.Enemies.SpawnDirector"/>
    /// picks from a roster of these and the <see cref="SpaceSurvivors.Enemies.EnemyBrain"/>
    /// applies them at spawn — time-based multipliers are layered on top by the
    /// <see cref="DifficultyConfig"/> (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Enemies/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Grunt";

        [Tooltip("Prefab must have EnemyBrain + HealthComponent + an IMoveStrategy + " +
                 "ContactDamage + Collider2D + Rigidbody2D + PoolHandle.")]
        public GameObject prefab;

        [Tooltip("Optional pooled one-shot VFX played where this enemy dies.")]
        public GameObject deathVfxPrefab;

        [Tooltip("Optional. If set, this enemy drops THIS pickup instead of the normal scrap " +
                 "drop (e.g. a boss XP orb). scrapValue below still applies as currency.")]
        public GameObject specialLootPrefab;

        [Header("Base stats (before time scaling)")]
        [Min(1f)] public float baseHealth = 20f;
        [Min(0f)] public float moveSpeed = 2.2f;
        [Min(0f)] public float contactDamage = 8f;
        [Tooltip("Seconds between contact-damage ticks while touching the player.")]
        [Min(0.05f)] public float contactInterval = 0.5f;

        [Header("Rewards (M5)")]
        [Min(0)] public int scrapValue = 1;

        [Header("Spawn eligibility")]
        [Tooltip("Seconds survived before this enemy can start appearing.")]
        [Min(0f)] public float earliestSpawnTime = 0f;
        [Tooltip("Relative chance vs other currently-eligible enemies.")]
        [Min(0f)] public float spawnWeight = 1f;

        [Tooltip("Recycle this enemy to the pool once the player has left it far behind " +
                 "(open arena). Turn OFF for bosses so they always pursue.")]
        public bool cullWhenFarOffscreen = true;
    }
}
