using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// A steering tweak layered on top of an enemy's base <see cref="IMoveStrategy"/>
    /// output. <see cref="EnemyBrain"/> collects every one on the GameObject and applies
    /// them in order, so behaviours compose (chase + separation + avoid-hazard + …) without
    /// any single class knowing about the others (AI_Guidelines §1).
    /// </summary>
    public interface IVelocityModifier
    {
        /// <param name="desiredVelocity">Velocity produced so far this frame.</param>
        /// <param name="selfPosition">This enemy's world position.</param>
        /// <param name="maxSpeed">The enemy's scaled top speed (for clamping).</param>
        Vector2 Modify(Vector2 desiredVelocity, Vector2 selfPosition, float maxSpeed, float deltaTime);
    }
}
