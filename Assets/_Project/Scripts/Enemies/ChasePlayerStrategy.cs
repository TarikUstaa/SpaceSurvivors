using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// The bread-and-butter swarm behaviour: head straight for the target at full speed.
    /// A tiny <see cref="_wobble"/> can be dialled in so a wall of enemies doesn't collapse
    /// into a single line.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChasePlayerStrategy : MonoBehaviour, IMoveStrategy
    {
        [Tooltip("Sideways sine-wave weave, in degrees. 0 = beeline.")]
        [SerializeField, Range(0f, 60f)] private float _wobble = 0f;
        [SerializeField] private float _wobbleSpeed = 3f;

        private float _phase;

        private void OnEnable() => _phase = Random.value * Mathf.PI * 2f;

        public Vector2 GetDesiredVelocity(Vector2 selfPosition, Vector2 targetPosition, float maxSpeed, float deltaTime)
        {
            Vector2 toTarget = targetPosition - selfPosition;
            if (toTarget.sqrMagnitude < 0.0001f) return Vector2.zero;

            Vector2 dir = toTarget.normalized;

            if (_wobble > 0f)
            {
                _phase += _wobbleSpeed * deltaTime;
                float offsetDeg = Mathf.Sin(_phase) * _wobble;
                dir = (Quaternion.Euler(0f, 0f, offsetDeg) * dir).normalized;
            }

            return dir * maxSpeed;
        }
    }
}
