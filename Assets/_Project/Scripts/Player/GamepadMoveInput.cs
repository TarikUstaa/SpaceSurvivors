using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// Concrete <see cref="IMoveInput"/> that reads the left stick — with the d-pad as a
    /// digital fallback for sticks that never quite recentre — via the Unity Input System's
    /// low-level <see cref="Gamepad"/> API. Same shape as <see cref="KeyboardMoveInput"/>: no
    /// .inputactions asset required, works from a blank scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class GamepadMoveInput : MonoBehaviour, IMoveInput
    {
        [Tooltip("Stick movement below this magnitude reads as centred — absorbs drift on a worn stick.")]
        [SerializeField, Range(0f, 0.5f)] private float _deadzone = 0.15f;

        public Vector2 MoveAxis { get; private set; }

        private void Update() => MoveAxis = Read(_deadzone);

        /// <summary>
        /// The pure read, with no component state — so <see cref="CombinedMoveInput"/> can ask
        /// "what is the gamepad doing right now" inside its own Update without depending on
        /// this component's Update having already run this frame (Unity does not guarantee
        /// sibling script order).
        /// </summary>
        internal static Vector2 Read(float deadzone)
        {
            var pad = Gamepad.current;
            if (pad == null) return Vector2.zero;

            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude < deadzone * deadzone)
            {
                float x = 0f, y = 0f;
                if (pad.dpad.left.isPressed)  x -= 1f;
                if (pad.dpad.right.isPressed) x += 1f;
                if (pad.dpad.down.isPressed)  y -= 1f;
                if (pad.dpad.up.isPressed)    y += 1f;
                stick = new Vector2(x, y);
            }

            // Clamp so diagonals are not faster than cardinals — matches KeyboardMoveInput,
            // and a full-deflection diagonal on a stick already reads near 1,1 without it.
            return stick.sqrMagnitude > 1f ? stick.normalized : stick;
        }
    }
}
