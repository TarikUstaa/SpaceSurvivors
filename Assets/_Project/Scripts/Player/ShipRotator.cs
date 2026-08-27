using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// OPTIONAL cosmetic component. Rotates the ship hull to face its current travel
    /// direction. Kept separate from <see cref="PlayerMovement"/> (AI_Guidelines §1) so
    /// facing behaviour can be swapped — face-movement, face-mouse, or none at all — with
    /// zero changes to the movement code. Remove this component for Vampire-Survivors-style
    /// fixed orientation.
    ///
    /// Reads velocity from <see cref="PlayerMovement.CurrentVelocity"/>; turn feel comes
    /// from <see cref="PlayerConfig"/> (no magic numbers, §3).
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [DisallowMultipleComponent]
    public class ShipRotator : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;

        private PlayerMovement _movement;
        private float _currentAngle;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _currentAngle = transform.eulerAngles.z;
            if (_config == null)
                Debug.LogError($"{nameof(ShipRotator)} on '{name}' has no PlayerConfig assigned.", this);
        }

        private void Update()
        {
            if (_config == null) return;

            Vector2 v = _movement.CurrentVelocity;
            if (v.sqrMagnitude < _config.minSpeedToTurn * _config.minSpeedToTurn)
                return; // Too slow / stopped — hold the last heading.

            float targetAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg
                                - _config.spriteForwardOffsetDegrees;

            _currentAngle = _config.turnSpeed <= 0f
                ? targetAngle
                : Mathf.MoveTowardsAngle(_currentAngle, targetAngle, _config.turnSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle);
        }
    }
}
