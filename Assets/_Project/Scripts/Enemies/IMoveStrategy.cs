using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Enemy steering behaviour, swappable per enemy type (chase, strafe, charge, orbit…).
    /// <see cref="EnemyBrain"/> resolves one from a sibling component and applies the result
    /// — the brain owns "when to move", the strategy owns "which way" (AI_Guidelines §1, §2).
    /// </summary>
    public interface IMoveStrategy
    {
        /// <returns>Desired world-space velocity for this frame.</returns>
        Vector2 GetDesiredVelocity(Vector2 selfPosition, Vector2 targetPosition, float maxSpeed, float deltaTime);
    }
}
