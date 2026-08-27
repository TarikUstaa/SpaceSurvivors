using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Immutable description of a single instance of damage. Passed by <c>in</c> to avoid
    /// copies in hot paths. Everything a receiver, a death handler, or a VFX hook might
    /// need to react (who, how much, where, which way) travels in here — so systems never
    /// have to reach back to the attacker (AI_Guidelines §1, loose coupling).
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>Positive amount of damage to apply.</summary>
        public readonly float Amount;

        /// <summary>The GameObject that caused this damage (may be null for world hazards).</summary>
        public readonly GameObject Source;

        /// <summary>World-space point of impact (for hit VFX / damage numbers).</summary>
        public readonly Vector2 HitPoint;

        /// <summary>Normalised direction the hit was travelling (for knockback / VFX).</summary>
        public readonly Vector2 HitDirection;

        public DamageInfo(float amount, GameObject source, Vector2 hitPoint, Vector2 hitDirection)
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector2.zero;
        }

        /// <summary>Convenience for simple cases with no positional data.</summary>
        public DamageInfo(float amount, GameObject source)
            : this(amount, source, Vector2.zero, Vector2.zero) { }
    }
}
