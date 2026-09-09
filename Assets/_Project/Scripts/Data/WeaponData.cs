using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Stats for one weapon. The <see cref="SpaceSurvivors.Combat.WeaponController"/> reads
    /// these at runtime; upgrades will later layer modifiers on top (M6). One asset per
    /// weapon archetype — Laser, Missile, ShieldPulse… (AI_Guidelines §3, zero magic numbers).
    /// </summary>
    /// <summary>How a weapon delivers its damage.</summary>
    public enum WeaponKind
    {
        /// <summary>Fires projectiles at the auto-aim target (Laser, Missile, …).</summary>
        Projectile,
        /// <summary>Orbs circle the ship and hurt whatever they touch (no firing).</summary>
        Orbital,
        /// <summary>Retired (was the Mine Layer, removed in G5). Kept so the enum indices of the
        /// values below it do not shift under already-serialised weapon assets.</summary>
        Trail,
        /// <summary>A persistent damage ring around the ship: every <see cref="WeaponData.cooldown"/>s it
        /// hits every enemy within <see cref="WeaponData.orbitRadius"/>. No projectiles, no aiming.</summary>
        Aura,
        /// <summary>An arc that strikes the nearest enemy, then leaps to the nearest not-yet-hit
        /// enemy within <see cref="WeaponData.chainRange"/>, up to <see cref="WeaponData.projectilesPerShot"/>
        /// targets, each hit doing <see cref="WeaponData.chainFalloff"/>× the last. Instant, no
        /// projectile. MultiShot feeds the jump count, so it grows into a swarm-clearer.</summary>
        Chain,
    }

    [CreateAssetMenu(menuName = "SpaceSurvivors/Combat/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Laser";
        public WeaponKind kind = WeaponKind.Projectile;

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

        [Header("Area of effect")]
        [Tooltip("On hit, also damage everything within this radius of the impact. 0 = no splash.")]
        [Min(0f)] public float explosionRadius = 0f;
        [Tooltip("Splash damage as a fraction of the direct-hit damage.")]
        [Range(0f, 1f)] public float splashDamageFraction = 0.6f;
        [Tooltip("Optional pooled VFX for the explosion (falls back to impactVfxPrefab).")]
        public GameObject explosionVfxPrefab;

        [Header("Orbital (kind = Orbital)")]
        [Tooltip("Orbs circle the ship at this radius (world units).")]
        [Min(0.5f)] public float orbitRadius = 2.2f;
        [Tooltip("Orbit angular speed, degrees per second (sign = direction).")]
        public float orbitDegreesPerSecond = 200f;
        [Tooltip("Seconds before an orb can hit the same enemy again.")]
        [Min(0.05f)] public float orbitHitInterval = 0.4f;

        [Header("Aura (kind = Aura)")]
        [Tooltip("Ring colour for an Aura weapon's glow. Alpha scales how strong the glow reads.")]
        public Color auraTint = new Color(0.4f, 0.85f, 1f, 0.16f);

        [Header("Chain (kind = Chain)")]
        [Tooltip("How far the arc can leap from one enemy to the next (world units). " +
                 "The first target is found within aimRange of the ship.")]
        [Min(0.5f)] public float chainRange = 3.5f;
        [Tooltip("Damage kept on each leap: 0.75 = every jump hits for 75% of the last.")]
        [Range(0.1f, 1f)] public float chainFalloff = 0.75f;
        [Tooltip("Arc colour. projectilesPerShot is the base number of targets (MultiShot adds more).")]
        public Color chainTint = new Color(0.6f, 0.85f, 1f, 1f);

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
