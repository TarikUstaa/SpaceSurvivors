namespace SpaceSurvivors.Stats
{
    /// <summary>
    /// Every tunable the upgrade system can touch. Components read their base value from a
    /// ScriptableObject and run it through the <see cref="StatSheet"/> for the final number,
    /// so upgrades never poke component internals (AI_Guidelines §1, §3).
    /// </summary>
    public enum StatId
    {
        MoveSpeed,          // multiplies PlayerConfig.moveSpeed
        MaxHealth,          // flat + / % on HealthData.maxHealth
        Damage,             // % on every weapon's damage
        FireRate,           // rate multiplier — 1.2 means shots come 20% faster
        ProjectileCount,    // flat + projectiles per shot
        ProjectileSpeed,    // % on projectile speed
        ProjectilePierce,   // flat + extra pierces
        ProjectileLifetime, // % on projectile lifetime
        PickupRadius,       // % on ScrapCollector radii
        XpGain,             // multiplier on collected XP
        ShieldCharges,      // flat + shield charges (0 base — the Shield upgrade unlocks it)
    }

    /// <summary>How a <see cref="StatModifier"/> combines into its stat.</summary>
    public enum ModifierOp
    {
        /// <summary>Added to the base before any percentages. (base + Σflat)</summary>
        Flat,
        /// <summary>Summed with other percent-adds, then applied once. ×(1 + Σpercent)</summary>
        PercentAdd,
        /// <summary>Multiplied in independently. ×∏(1 + value)  — use for rare big spikes.</summary>
        Multiplier,
    }
}
