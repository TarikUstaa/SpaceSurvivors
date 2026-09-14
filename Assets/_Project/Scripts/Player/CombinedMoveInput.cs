using UnityEngine;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// Feeds <see cref="PlayerMovement"/> from whichever device the player is actually
    /// touching right now. <see cref="KeyboardMoveInput"/> and <see cref="GamepadMoveInput"/>
    /// each stay single-responsibility — this only picks between their two already-normalised
    /// vectors, gamepad first — so plugging in a controller mid-run, or letting go of it, just
    /// works with no settings toggle and no component to swap.
    ///
    /// <para>Drop this on the player in place of <see cref="KeyboardMoveInput"/>; nothing else
    /// changes, since <see cref="PlayerMovement"/> only ever depends on <see cref="IMoveInput"/>
    /// (AI_Guidelines §1, §5).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class CombinedMoveInput : MonoBehaviour, IMoveInput
    {
        [Tooltip("Stick movement below this magnitude reads as centred — absorbs drift on a worn stick.")]
        [SerializeField, Range(0f, 0.5f)] private float _gamepadDeadzone = 0.15f;

        public Vector2 MoveAxis { get; private set; }

        private void Update()
        {
            Vector2 pad = GamepadMoveInput.Read(_gamepadDeadzone);
            MoveAxis = pad.sqrMagnitude > 0.0001f ? pad : KeyboardMoveInput.Read();
        }
    }
}
