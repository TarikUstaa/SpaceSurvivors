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

        [Tooltip("Optional. Empty = auto-resolve a SpriteRenderer on this GameObject or its children.")]
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("Optional. Empty = auto-resolve a HitFlash on this GameObject.")]
        [SerializeField] private HitFlash _hitFlash;

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

        private Color _baseColor = Color.white;
        private Vector3 _baseScale = Vector3.one;

        /// <summary>Raised the moment this enemy's health hits zero, before it despawns.</summary>
        public event Action<EnemyBrain, DamageInfo> Killed;

        public EnemyData Data => _data;
        public bool IsActive => _active;

        /// <summary>True for the lifetime of this spawn if it won an elite roll (see
        /// <see cref="DifficultyConfig.eliteChance"/>). Reset every <see cref="Initialize"/>.</summary>
        public bool IsElite { get; private set; }

        /// <summary>How much this kill's scrap/XP payout should be scaled by. 1 unless elite.</summary>
        public float ScrapMultiplier { get; private set; } = 1f;

        private float _speedBuffMultiplier = 1f;
        private float _speedBuffExpiry = -1f;

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

            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_hitFlash == null) _hitFlash = GetComponent<HitFlash>();
            if (_renderer != null) _baseColor = _renderer.color;
            _baseScale = transform.localScale;

            _body.gravityScale = 0f;
            _body.freezeRotation = true;

            if (_move == null)
                Debug.LogError($"{nameof(EnemyBrain)} on '{name}' has no IMoveStrategy component.", this);
        }

        private void OnEnable() => _health.Died += HandleDied;
        private void OnDisable() => _health.Died -= HandleDied;

        /// <summary>Called by the spawn director right after the pool hands out this instance.
        /// <paramref name="elite"/> is null for every ordinary spawn (bosses and split-off
        /// children never roll elite) — <see cref="EliteModifiers.None"/> is applied in that case.</summary>
        public void Initialize(Transform target, EnemyData data, float scaledHealth, float speedMultiplier,
                               Action<EnemyBrain> onReleased, EliteModifiers? elite = null)
        {
            EliteModifiers mods = elite ?? EliteModifiers.None;

            _target = target;
            _data = data;
            _onReleased = onReleased;
            _speed = data.moveSpeed * speedMultiplier;

            _health.SetMaxHealth(scaledHealth, healToFull: true);
            _contact?.Configure(data.contactDamage * mods.DamageMultiplier, data.contactInterval);

            IsElite = mods.IsElite;
            ScrapMultiplier = mods.ScrapMultiplier;
            ApplyEliteVisual(mods);

            _body.linearVelocity = Vector2.zero;
            _active = true;
        }

        /// <summary>Pooled instances get reused as both elite and non-elite over a run, so this
        /// always sets an absolute state (tint/scale) rather than only changing something when
        /// <paramref name="mods"/> is elite — otherwise a former elite handed back out as a
        /// regular enemy would still look elite.</summary>
        private void ApplyEliteVisual(EliteModifiers mods)
        {
            Color colour = mods.IsElite ? mods.Tint : _baseColor;
            if (_renderer != null) _renderer.color = colour;
            _hitFlash?.SetBaseColor(colour);

            transform.localScale = _baseScale * (mods.IsElite ? mods.Scale : 1f);
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

            if (_speedBuffExpiry > 0f && Time.time >= _speedBuffExpiry)
            {
                _speedBuffMultiplier = 1f;
                _speedBuffExpiry = -1f;
            }

            float dt = Time.fixedDeltaTime;
            Vector2 velocity = _move != null
                ? _move.GetDesiredVelocity(_body.position, (Vector2)_target.position, _speed * _speedBuffMultiplier, dt)
                : Vector2.zero;

            // Layer steering tweaks (separation, hazard-avoidance, …) on top of the base move.
            if (_modifiers != null)
                for (int i = 0; i < _modifiers.Length; i++)
                    if (_modifiers[i] is MonoBehaviour mb && mb != null)
                        velocity = _modifiers[i].Modify(velocity, _body.position, _speed, dt);

            _body.linearVelocity = velocity;
        }

        /// <summary>Temporarily scale move speed (e.g. a support enemy's aura). If already
        /// buffed, keeps the stronger multiplier and refreshes the expiry — a unit standing
        /// in two overlapping auras isn't stacked, just kept topped up.</summary>
        public void ApplySpeedBuff(float multiplier, float duration)
        {
            _speedBuffMultiplier = Mathf.Max(_speedBuffMultiplier, multiplier);
            _speedBuffExpiry = Time.time + duration;
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
            _speedBuffMultiplier = 1f;
            _speedBuffExpiry = -1f;
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
