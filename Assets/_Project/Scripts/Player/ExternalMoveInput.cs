using UnityEngine;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// An <see cref="IMoveInput"/> whose vector is pushed in from outside — a replay, a test
    /// harness, or the M16 balance autopilot. Set <see cref="MoveAxis"/> each frame; if nothing
    /// updates it the ship simply coasts. Inert unless something drives it, so it is safe to
    /// leave on a prefab (AI_Guidelines §1, §5 — the "AI / replay provider" the interface
    /// comment anticipates).
    /// </summary>
    [DisallowMultipleComponent]
    public class ExternalMoveInput : MonoBehaviour, IMoveInput
    {
        public Vector2 MoveAxis { get; set; }
    }
}
