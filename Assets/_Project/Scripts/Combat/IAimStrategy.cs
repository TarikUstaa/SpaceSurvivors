using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Decides which way a weapon should fire. Swappable like <c>IMoveInput</c>:
    /// nearest-enemy, toward-mouse, ship-forward, spin — the <see cref="WeaponController"/>
    /// resolves one from a sibling component and never hard-codes aiming (AI_Guidelines §1).
    /// </summary>
    public interface IAimStrategy
    {
        /// <param name="origin">World position the shot originates from.</param>
        /// <param name="range">Search radius the weapon cares about.</param>
        /// <param name="direction">Normalised aim direction (only valid when the method returns true).</param>
        bool TryGetAimDirection(Vector2 origin, float range, out Vector2 direction);
    }
}
