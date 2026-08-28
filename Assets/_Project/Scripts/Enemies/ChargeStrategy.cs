using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Hit-and-run dasher. Cycles Approach → Wind-up (brief stop, locks a direction) → Dash
    /// (fast, straight, committed) → Recover (drift) and repeats. The lock means a dash can
    /// be side-stepped, which is the whole point of the archetype (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class ChargeStrategy : MonoBehaviour, IMoveStrategy
    {
        private enum Phase { Approach, Windup, Dash, Recover }

        [Tooltip("Starts winding up once this close to the target.")]
        [SerializeField, Min(1f)] private float _windupRange = 4.5f;
        [Tooltip("Seconds of telegraph before the dash fires.")]
        [SerializeField, Min(0f)] private float _windupTime = 0.6f;
        [Tooltip("Dash speed as a multiple of normal move speed.")]
        [SerializeField, Min(1f)] private float _dashSpeedMultiplier = 4.5f;
        [Tooltip("Seconds the dash lasts.")]
        [SerializeField, Min(0.05f)] private float _dashDuration = 0.45f;
        [Tooltip("Seconds of slow drift after a dash before approaching again.")]
        [SerializeField, Min(0f)] private float _recoverTime = 0.9f;
        [Tooltip("Approach / recover speed as a fraction of normal move speed.")]
        [SerializeField, Range(0.1f, 1f)] private float _cruiseFraction = 0.7f;

        private Phase _phase = Phase.Approach;
        private float _timer;
        private Vector2 _dashDir;

        private void OnEnable()
        {
            _phase = Phase.Approach;
            _timer = 0f;
        }

        public Vector2 GetDesiredVelocity(Vector2 selfPosition, Vector2 targetPosition, float maxSpeed, float deltaTime)
        {
            Vector2 toTarget = targetPosition - selfPosition;
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.0001f ? toTarget / dist : Vector2.right;

            switch (_phase)
            {
                case Phase.Approach:
                    if (dist <= _windupRange) { _phase = Phase.Windup; _timer = _windupTime; }
                    return dir * (maxSpeed * _cruiseFraction);

                case Phase.Windup:
                    _timer -= deltaTime;
                    _dashDir = dir; // keep re-aiming until the moment it commits
                    if (_timer <= 0f) { _phase = Phase.Dash; _timer = _dashDuration; }
                    return Vector2.zero; // telegraph: hold still

                case Phase.Dash:
                    _timer -= deltaTime;
                    if (_timer <= 0f) { _phase = Phase.Recover; _timer = _recoverTime; }
                    return _dashDir * (maxSpeed * _dashSpeedMultiplier);

                default: // Recover
                    _timer -= deltaTime;
                    if (_timer <= 0f) _phase = Phase.Approach;
                    return dir * (maxSpeed * _cruiseFraction * 0.4f);
            }
        }
    }
}
