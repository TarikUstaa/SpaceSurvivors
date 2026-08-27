using UnityEngine;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// Abstraction over "where does the movement vector come from".
    /// Movement code depends on this, NOT on the keyboard directly, so we can later swap
    /// in an InputActions-based provider, an AI/bot provider, or a replay provider for
    /// tests without editing <see cref="PlayerMovement"/> (AI_Guidelines §1, §5).
    /// </summary>
    public interface IMoveInput
    {
        /// <summary>Normalised-ish move intent. Magnitude 0..1, (0,0) when idle.</summary>
        Vector2 MoveAxis { get; }
    }
}
