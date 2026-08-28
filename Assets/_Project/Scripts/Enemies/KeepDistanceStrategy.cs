using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Kiting behaviour for ranged enemies: close to <see cref="_preferredRange"/>, then hold
    /// that distance while strafing sideways so it's a moving target. Pairs with
    /// <see cref="RangedAttack"/> (AI_Guidelines §1 — one strategy, one job).
    /// </summary>
    [DisallowMultipleComponent]
    public class KeepDistanceStrategy : MonoBehaviour, IMoveStrategy
    {
        [Tooltip("Distance from the target this enemy tries to hold.")]
        [SerializeField, Min(1f)] private float _preferredRange = 6f;
        [Tooltip("Dead-band around the preferred range where it just strafes.")]
        [SerializeField, Min(0.1f)] private float _band = 1.5f;
        [Tooltip("Sideways strafe speed as a fraction of max speed.")]
        [SerializeField, Range(0f, 1f)] private float _strafeFraction = 0.6f;
        [Tooltip("Seconds between strafe-direction flips.")]
        [SerializeField, Min(0.5f)] private float _strafeFlipInterval = 2.5f;

        private int _strafeDir = 1;
        private float _flipTimer;

        private void OnEnable()
        {
            _strafeDir = Random.value < 0.5f ? -1 : 1;
            _flipTimer = _strafeFlipInterval * Random.Range(0.5f, 1.5f);
        }

        public Vector2 GetDesiredVelocity(Vector2 selfPosition, Vector2 targetPosition, float maxSpeed, float deltaTime)
        {
            Vector2 toTarget = targetPosition - selfPosition;
            float dist = toTarget.magnitude;
            if (dist < 0.0001f) return Vector2.zero;

            Vector2 inward = toTarget / dist;
            Vector2 perp = new(-inward.y, inward.x);

            _flipTimer -= deltaTime;
            if (_flipTimer <= 0f)
            {
                _strafeDir = -_strafeDir;
                _flipTimer = _strafeFlipInterval * Random.Range(0.7f, 1.3f);
            }

            Vector2 radial = Vector2.zero;
            if (dist > _preferredRange + _band) radial = inward;                 // too far — approach
            else if (dist < _preferredRange - _band) radial = -inward;           // too close — back off

            Vector2 strafe = perp * (_strafeDir * _strafeFraction);
            return (radial + strafe).normalized * maxSpeed;
        }
    }
}
