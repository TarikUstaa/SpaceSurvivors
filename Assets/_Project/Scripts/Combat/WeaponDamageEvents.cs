using System;
using SpaceSurvivors.Data;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// One static event every landed player-weapon hit raises, for the run-end damage
    /// breakdown. Lives here at the Combat layer — not on <see cref="Core.DamageInfo"/> —
    /// specifically because it carries a <see cref="WeaponData"/> and Core has zero
    /// dependencies by design (AI_Guidelines §6); tagging DamageInfo with a weapon would have
    /// forced Core to depend on Data.
    ///
    /// <para>Raised only by the five weapon-driving scripts (Projectile, ChainLightning,
    /// AuraWeapon, OrbHit, the Aoe splash helper) right where each already checks
    /// <c>TakeDamage</c>'s return value — enemies carry no armour stat, so the amount passed
    /// into that call is exactly what landed, no separate "actual applied" lookup needed.
    /// Never raised for enemy attacks, hazards, or space events.</para>
    /// </summary>
    public static class WeaponDamageEvents
    {
        public static event Action<WeaponData, float> Dealt;

        public static void Raise(WeaponData weapon, float amount)
        {
            if (weapon != null && amount > 0f) Dealt?.Invoke(weapon, amount);
        }
    }
}
