using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// OPTIONAL cosmetic component. Turns the ship hull to face where it's heading. Kept
    /// separate from <see cref="PlayerMovement"/> (AI_Guidelines §1) so facing behaviour can
    /// be swapped — face-movement, face-mouse, or none — without touching movement code.
    /// Remove this component for Vampire-Survivors-style fixed orientation.
    ///
    /// SMOOTHNESS
    /// ----------
    /// * Steers toward the raw <see cref="IMoveInput"/> direction, not the Rigidbody
    ///   velocity. Input is a clean, full-rate heading; velocity is stepped at the physics
    ///   rate and passes through zero on a reversal, which made the old version snap 180°.
    /// * Rotates through <see cref="Rigidbody2D.MoveRotation"/> in <see cref="FixedUpdate"/>
    ///   so the hull's rotation interpolates in lock-step with its position (the body has
    ///   Interpolate on). Writing <c>transform.rotation</c> from Update fights that.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class ShipRotator : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [Tooltip("Optional. Empty = auto-resolve an IMoveInput on this GameObject.")]
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        private PlayerMovement _movement;
        private Rigidbody2D _body;
        private IMoveInput _input;
        private float _currentAngle;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _body = GetComponent<Rigidbody2D>();
            _input = _inputSourceBehaviour as IMoveInput ?? GetComponent<IMoveInput>();
            _currentAngle = transform.eulerAngles.z;

            if (_config == null)
                Debug.LogError($"{nameof(ShipRotator)} on '{name}' has no PlayerConfig assigned.", this);
        }

        private void FixedUpdate()
        {
            if (_config == null) return;

            // Prefer the live input heading; fall back to travel direction while coasting.
            Vector2 heading = _input != null ? _input.MoveAxis : Vector2.zero;
            if (heading.sqrMagnitude < 0.0001f)
            {
                Vector2 v = _movement.CurrentVelocity;
                if (v.sqrMagnitude < _config.minSpeedToTurn * _config.minSpeedToTurn)
                {
                    _currentAngle = _body.rotation; // stay in sync, hold heading
                    return;
                }
                heading = v;
            }

            float targetAngle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg
                                - _config.spriteForwardOffsetDegrees;

            _currentAngle = _config.turnSpeed <= 0f
                ? targetAngle
                : Mathf.MoveTowardsAngle(_currentAngle, targetAngle, _config.turnSpeed * Time.fixedDeltaTime);

            _body.MoveRotation(_currentAngle);
        }
    }
}
