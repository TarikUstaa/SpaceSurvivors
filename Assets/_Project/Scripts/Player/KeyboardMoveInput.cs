using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// Concrete <see cref="IMoveInput"/> that reads WASD / arrow keys via the Unity Input
    /// System's low-level <see cref="Keyboard"/> API. No .inputactions asset required, so
    /// the project runs from a blank scene. Swap this component out for an InputActions
    /// provider later — nothing else needs to change.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeyboardMoveInput : MonoBehaviour, IMoveInput
    {
        public Vector2 MoveAxis { get; private set; }

        private void Update() => MoveAxis = Read();

        /// <summary>
        /// The pure read, with no component state — so <see cref="CombinedMoveInput"/> can ask
        /// "what is the keyboard doing right now" inside its own Update without depending on
        /// this component's Update having already run this frame (Unity does not guarantee
        /// sibling script order).
        /// </summary>
        internal static Vector2 Read()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y += 1f;

            var v = new Vector2(x, y);
            // Clamp so diagonals are not faster than cardinals.
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
    }
}
