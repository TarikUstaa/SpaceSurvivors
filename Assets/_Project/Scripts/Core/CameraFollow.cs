using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Smoothly keeps the camera centred on a target (the player) for an open, scrolling
    /// arena — Vampire-Survivors style, no screen bounds. Pure view glue: it never touches
    /// gameplay, so it lives in Core and any scene can drop it on a camera (AI_Guidelines §1).
    ///
    /// The <see cref="Enemies.SpawnDirector"/> already spawns relative to the camera, so once
    /// the camera follows the player, enemies automatically ring-spawn around the player.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [Tooltip("Approx. seconds for the camera to catch up to the target. 0 = snap.")]
        [SerializeField, Min(0f)] private float _smoothTime = 0.12f;

        [Tooltip("How far ahead of the target's motion the camera leads, in seconds of travel.")]
        [SerializeField, Min(0f)] private float _lookAhead = 0.09f;

        [Tooltip("Max look-ahead offset in world units, so fast movement doesn't shove the view too far.")]
        [SerializeField, Min(0f)] private float _maxLookAhead = 1.4f;

        private Vector3 _velocity;
        private Vector3 _lookAheadPos;
        private Rigidbody2D _targetBody;

        public void SetTarget(Transform target)
        {
            _target = target;
            _targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
            if (target != null) SnapToTarget();
        }

        private void Awake()
        {
            if (_target != null) _targetBody = _target.GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => SnapToTarget();

        private void SnapToTarget()
        {
            if (_target == null) return;
            var p = _target.position;
            p.z = transform.position.z;
            transform.position = p;
            _lookAheadPos = Vector3.zero;
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 lead = Vector3.zero;
            if (_targetBody != null && _lookAhead > 0f)
            {
                Vector3 desiredLead = (Vector3)_targetBody.linearVelocity * _lookAhead;
                desiredLead = Vector3.ClampMagnitude(desiredLead, _maxLookAhead);
                _lookAheadPos = Vector3.Lerp(_lookAheadPos, desiredLead, 1f - Mathf.Exp(-6f * Time.deltaTime));
                lead = _lookAheadPos;
            }

            Vector3 goal = _target.position + lead;
            goal.z = transform.position.z;

            transform.position = _smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, goal, ref _velocity, _smoothTime)
                : goal;
        }
    }
}
