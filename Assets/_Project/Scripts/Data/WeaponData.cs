using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Stats for one weapon. The <see cref="SpaceSurvivors.Combat.WeaponController"/> reads
    /// these at runtime; upgrades will later layer modifiers on top (M6). One asset per
    /// weapon archetype — Laser, Missile, ShieldPulse… (AI_Guidelines §3, zero magic numbers).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Combat/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Laser";

        [Header("Projectile")]
        [Tooltip("Prefab must have a Projectile + Collider2D + Rigidbody2D + PoolHandle.")]
        public GameObject projectilePrefab;
        [Min(0f)] public float projectileSpeed = 12f;
        [Min(0.05f)] public float projectileLifetime = 2f;

        [Tooltip("Optional pooled one-shot VFX played where a projectile hits an enemy.")]
        public GameObject impactVfxPrefab;

        [Header("Firing")]
        [Tooltip("Seconds between shots. Lower = faster fire rate.")]
        [Min(0.02f)] public float cooldown = 0.5f;
        [Tooltip("Damage per projectile hit.")]
        [Min(0f)] public float damage = 10f;
        [Tooltip("How many projectiles leave per shot.")]
        [Min(1)] public int projectilesPerShot = 1;
        [Tooltip("Total fan angle (degrees) the projectiles spread across. 0 = all parallel.")]
        [Min(0f)] public float spreadAngle = 0f;
        [Tooltip("Extra targets a projectile passes through before despawning. 0 = hits one.")]
        [Min(0)] public int pierce = 0;

        [Header("Targeting")]
        [Tooltip("Radius the auto-aim searches for enemies. Also the projectile cull range guide.")]
        [Min(0f)] public float aimRange = 8f;

        [Header("Evolution (M6)")]
        [Tooltip("The stronger weapon this becomes. Null = cannot evolve.")]
        public WeaponData evolvesInto;
        [Tooltip("This passive upgrade must be at max stacks before the evolution is offered.")]
        public UpgradeData evolutionCatalyst;
    }
}
