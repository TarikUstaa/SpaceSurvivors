using System;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Orchestrates one enemy by composing the shared pieces — it holds almost no logic
    /// itself (AI_Guidelines §1):
    ///   * <see cref="HealthComponent"/> — HP &amp; death (same component the player uses)
    ///   * <see cref="IMoveStrategy"/>   — steering (swappable per prefab)
    ///   * <see cref="ContactDamage"/>   — touching the player hurts
    ///   * <see cref="PoolHandle"/>      — return to pool on death (never Destroy, §4)
    ///
    /// The <see cref="SpawnDirector"/> calls <see cref="Initialize"/> every spawn to inject
    /// the target and the time-scaled stats, and passes a release callback so it can keep
    /// its alive-count accurate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class EnemyBrain : MonoBehaviour, IPoolable
    {
        [Tooltip("Distance from the target past which this enemy is recycled (open arena). " +
                 "Only applies when its EnemyData has cullWhenFarOffscreen = true.")]
        [SerializeField, Min(10f)] private float _farCullRadius = 45f;

        private Rigidbody2D _body;
        private HealthComponent _health;
        private IMoveStrategy _move;
        private IVelocityModifier[] _modifiers;
        private ContactDamage _contact;
        private PoolHandle _handle;

        private Transform _target;
        private EnemyData _data;
        private float _speed;
        private bool _active;
        private Action<EnemyBrain> _onReleased;

        /// <summary>Raised the moment this enemy's health hits zero, before it despawns.</summary>
        public event Action<EnemyBrain, DamageInfo> Killed;

        public EnemyData Data => _data;
        public bool IsActive => _active;

        /// <summary>The thing this enemy is hunting (the player). Null until <see cref="Initialize"/>.
        /// Read by abilities like <see cref="RangedAttack"/>.</summary>
        public Transform Target => _target;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<HealthComponent>();
            _move = GetComponent<IMoveStrategy>();
            _modifiers = GetComponents<IVelocityModifier>();
            _contact = GetComponent<ContactDamage>();
            _handle = GetComponent<PoolHandle>();

            _body.gravityScale = 0f;
            _body.freezeRotation = true;

            if (_move == null)
                Debug.LogError($"{nameof(EnemyBrain)} on '{name}' has no IMoveStrategy component.", this);
        }

        private void OnEnable() => _health.Died += HandleDied;
        private void OnDisable() => _health.Died -= HandleDied;

        /// <summary>Called by the spawn director right after the pool hands out this instance.</summary>
        public void Initialize(Transform target, EnemyData data, float scaledHealth, float speedMultiplier,
                               Action<EnemyBrain> onReleased)
        {
            _target = target;
            _data = data;
            _onReleased = onReleased;
            _speed = data.moveSpeed * speedMultiplier;

            _health.SetMaxHealth(scaledHealth, healToFull: true);
            _contact?.Configure(data.contactDamage, data.contactInterval);

            _body.linearVelocity = Vector2.zero;
            _active = true;
        }

        private void FixedUpdate()
        {
            // Defensive: a burst of NREs was seen here at very high enemy counts as a run
            // ended. Guard the cached refs (and skip any torn-down modifier below) so an
            // instance getting one extra physics tick during teardown can't spam the console.
            if (!_active || _target == null || _body == null) return;

            if (_data != null && _data.cullWhenFarOffscreen)
            {
                float sqrFar = _farCullRadius * _farCullRadius;
                if (((Vector2)_target.position - _body.position).sqrMagnitude > sqrFar)
                {
                    _handle.Despawn();   // OnDespawned fires the release callback; no Killed event, no loot
                    return;
                }
            }

            float dt = Time.fixedDeltaTime;
            Vector2 velocity = _move != null
                ? _move.GetDesiredVelocity(_body.position, (Vector2)_target.position, _speed, dt)
                : Vector2.zero;

            // Layer steering tweaks (separation, hazard-avoidance, …) on top of the base move.
            if (_modifiers != null)
                for (int i = 0; i < _modifiers.Length; i++)
                    if (_modifiers[i] is MonoBehaviour mb && mb != null)
                        velocity = _modifiers[i].Modify(velocity, _body.position, _speed, dt);

            _body.linearVelocity = velocity;
        }

        private void HandleDied(DamageInfo info)
        {
            if (!_active) return;
            _active = false;
            Killed?.Invoke(this, info);
            _handle.Despawn();
        }

        // ---- Pool lifecycle ----
        public void OnSpawned()
        {
            // Stay dormant until Initialize() supplies a target and stats.
            _active = false;
            _body.linearVelocity = Vector2.zero;
        }

        public void OnDespawned()
        {
            _active = false;
            _body.linearVelocity = Vector2.zero;
            _target = null;

            var cb = _onReleased;
            _onReleased = null;
            cb?.Invoke(this);
        }
    }
}
