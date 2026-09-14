using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// What one elite roll changes about a spawn, bundled so <see cref="EnemyBrain.Initialize"/>
    /// doesn't grow another loose parameter every time an elite trait is added. Health is not
    /// in here — <see cref="SpawnDirector"/> already bakes every multiplier into one HP number
    /// before calling Initialize, and elite health rides the same path.
    /// </summary>
    public readonly struct EliteModifiers
    {
        public readonly bool IsElite;
        public readonly float DamageMultiplier;
        public readonly float ScrapMultiplier;
        public readonly float Scale;
        public readonly Color Tint;

        /// <summary>The non-elite case. A default(EliteModifiers) would zero every multiplier
        /// out (including scale), which would shrink a normal enemy to nothing — always go
        /// through this instead of relying on the struct's implicit default.</summary>
        public static readonly EliteModifiers None = new(false, 1f, 1f, 1f, Color.white);

        public EliteModifiers(bool isElite, float damageMultiplier, float scrapMultiplier, float scale, Color tint)
        {
            IsElite = isElite;
            DamageMultiplier = damageMultiplier;
            ScrapMultiplier = scrapMultiplier;
            Scale = scale;
            Tint = tint;
        }
    }
}
