using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Player
{
    /// <summary>
    /// Moves the player ship in 2D based on an <see cref="IMoveInput"/> source and tuning
    /// from a <see cref="PlayerConfig"/> ScriptableObject.
    ///
    /// ARCHITECTURE NOTES
    /// ------------------
    /// * Single responsibility: this component ONLY moves the body. It does not read raw
    ///   keys (that's <see cref="KeyboardMoveInput"/>), hold HP, or fire weapons. Those are
    ///   separate components on the same GameObject (AI_Guidelines §1).
    /// * Data-driven: every number comes from <see cref="_config"/>. No magic numbers here
    ///   (AI_Guidelines §3).
    /// * Decoupled input: depends on the <see cref="IMoveInput"/> interface, resolved from a
    ///   sibling component. Any provider (keyboard, gamepad, AI, replay) works unchanged.
    /// * Physics-based: writes to <see cref="Rigidbody2D.linearVelocity"/> in FixedUpdate so
    ///   collisions with enemies/walls resolve correctly. The Rigidbody2D should be
    ///   Dynamic, Gravity Scale 0, Freeze Rotation Z.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerConfig _config;

        [Header("Input source (optional)")]
        [Tooltip("Leave empty to auto-resolve an IMoveInput component on this GameObject.")]
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        [Header("Obstacles")]
        [Tooltip("Layers the ship can't fly through (M15 asteroids / terrain). Empty = no blocking.")]
        [SerializeField] private LayerMask _obstacleMask;

        private IMoveInput _input;
        private Rigidbody2D _body;
        private Camera _camera;

        private ContactFilter2D _obstacleFilter;
        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[8];
        private const float ObstacleSkin = 0.03f;

        [Tooltip("Optional. Empty = auto-resolve a StatSheet on this GameObject.")]
        [SerializeField] private SpaceSurvivors.Stats.StatSheet _stats;

        // Exposed for other systems (e.g. thruster VFX, weapon lead) without coupling.
        public Vector2 CurrentVelocity => _body != null ? _body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            // Cache references once — never GetComponent in Update (AI_Guidelines §4).
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            // Rotation is driven by ShipRotator via MoveRotation, so it must NOT be frozen.
            // The body is Kinematic — collisions can't spin it anyway.
            _body.constraints = RigidbodyConstraints2D.None;

            _input = _inputSourceBehaviour as IMoveInput ?? GetComponent<IMoveInput>();
            if (_stats == null) _stats = GetComponent<SpaceSurvivors.Stats.StatSheet>();
            _camera = Camera.main;

            _obstacleFilter = new ContactFilter2D { useTriggers = false, useLayerMask = true };
            _obstacleFilter.SetLayerMask(_obstacleMask);

            if (_config == null)
                Debug.LogError($"{nameof(PlayerMovement)} on '{name}' has no PlayerConfig assigned.", this);
            if (_input == null)
                Debug.LogError($"{nameof(PlayerMovement)} on '{name}' found no IMoveInput source.", this);
        }

        private void FixedUpdate()
        {
            if (_config == null || _input == null) return;

            float dt = Time.fixedDeltaTime;
            float speed = _stats != null
                ? _stats.Modify(SpaceSurvivors.Stats.StatId.MoveSpeed, _config.moveSpeed)
                : _config.moveSpeed;
            Vector2 desiredVelocity = _input.MoveAxis * speed;
            Vector2 current = _body.linearVelocity;

            // Accelerate toward the target, decelerate toward zero when idle.
            bool hasInput = _input.MoveAxis.sqrMagnitude > 0.0001f;
            float rate = hasInput ? _config.acceleration : _config.deceleration;
            Vector2 newVelocity = Vector2.MoveTowards(current, desiredVelocity, rate * dt);

            _body.linearVelocity = Deflect(newVelocity, dt);

            if (_config.clampToScreen)
                ClampToScreen();
        }

        /// <summary>
        /// The ship's body is Kinematic, so the physics solver won't stop it at an asteroid.
        /// Sweep the collider along the intended move and cancel the velocity component that
        /// drives into any obstacle surface — the ship slides along the rock instead of
        /// passing through it.
        /// </summary>
        private Vector2 Deflect(Vector2 velocity, float dt)
        {
            if (_obstacleMask.value == 0 || velocity.sqrMagnitude < 0.0001f) return velocity;

            float dist = velocity.magnitude * dt + ObstacleSkin;
            int n = _body.Cast(velocity.normalized, _obstacleFilter, _castHits, dist);
            for (int i = 0; i < n; i++)
            {
                Vector2 nrm = _castHits[i].normal;
                float into = Vector2.Dot(velocity, -nrm);
                if (into > 0f) velocity += nrm * into;
            }
            return velocity;
        }

        /// <summary>Keeps the ship inside the camera view minus a configurable margin.</summary>
        private void ClampToScreen()
        {
            if (_camera == null || !_camera.orthographic) return;

            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;
            Vector3 c = _camera.transform.position;
            float pad = _config.screenEdgePadding;

            Vector2 pos = _body.position;
            float clampedX = Mathf.Clamp(pos.x, c.x - halfW + pad, c.x + halfW - pad);
            float clampedY = Mathf.Clamp(pos.y, c.y - halfH + pad, c.y + halfH - pad);

            if (!Mathf.Approximately(clampedX, pos.x) || !Mathf.Approximately(clampedY, pos.y))
            {
                _body.position = new Vector2(clampedX, clampedY);
                // Kill outward velocity so we don't "stick" jittering against the edge.
                Vector2 v = _body.linearVelocity;
                if (!Mathf.Approximately(clampedX, pos.x)) v.x = 0f;
                if (!Mathf.Approximately(clampedY, pos.y)) v.y = 0f;
                _body.linearVelocity = v;
            }
        }
    }
}
