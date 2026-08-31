using System;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// A solid piece of terrain (M15): an asteroid, a wreck, a scrap cache. Kinematic body so
    /// it physically blocks the Dynamic player and enemies while we still move it ourselves
    /// (spawn placement + optional drift). Every obstacle carries a <see cref="HealthComponent"/>:
    ///
    ///  * <b>Non-destructible</b> — huge HP, <see cref="HealthComponent.Died"/> never fires.
    ///    Player shots just vanish into it (the projectile's own IDamageable path handles that,
    ///    no projectile code change).
    ///  * <b>Destructible</b> — modest HP; on death it raises <see cref="Destroyed"/> so the
    ///    <see cref="EnvironmentDirector"/> can pop debris VFX and drop <see cref="ScrapReward"/>
    ///    scrap. The prefab itself stays loot-agnostic (AI_Guidelines §1).
    ///
    /// Pool-friendly: fully reset on spawn, never Destroyed (§4).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class Obstacle : MonoBehaviour, IPoolable
    {
        [Tooltip("World units/sec the obstacle drifts. 0 = static.")]
        [SerializeField, Min(0f)] private float _driftSpeed;
        [Tooltip("Degrees/sec idle spin. 0 = none.")]
        [SerializeField] private float _spinSpeed;
        [Tooltip("Scrap handed out when a destructible obstacle is cracked. 0 = none (plain rock).")]
        [SerializeField, Min(0)] private int _scrapReward;

        /// <summary>(this obstacle, world position, the killing blow). Only ever fires for a
        /// destructible obstacle — the director listens to spawn debris + loot.</summary>
        public event Action<Obstacle, Vector2, DamageInfo> Destroyed;

        public int ScrapReward => _scrapReward;

        private Rigidbody2D _body;
        private HealthComponent _health;
        private PoolHandle _handle;
        private Vector2 _drift;
        private bool _dead;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _health = GetComponent<HealthComponent>();
            _handle = GetComponent<PoolHandle>();
        }

        private void OnEnable() => _health.Died += HandleDied;
        private void OnDisable() => _health.Died -= HandleDied;

        public void OnSpawned()
        {
            _dead = false;
            float a = UnityEngine.Random.value * Mathf.PI * 2f;
            _drift = _driftSpeed > 0f
                ? new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _driftSpeed
                : Vector2.zero;
            _body.linearVelocity = Vector2.zero;
            transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        }

        public void OnDespawned()
        {
            _drift = Vector2.zero;
            _body.linearVelocity = Vector2.zero;
        }

        /// <summary>Override the random idle drift with a specific velocity (M18 meteor shower).</summary>
        public void SetDrift(Vector2 velocity) => _drift = velocity;

        private void FixedUpdate()
        {
            if (_drift != Vector2.zero)
                _body.MovePosition(_body.position + _drift * Time.fixedDeltaTime);
            if (_spinSpeed != 0f)
                _body.MoveRotation(_body.rotation + _spinSpeed * Time.fixedDeltaTime);
        }

        private void HandleDied(DamageInfo info)
        {
            if (_dead) return;
            _dead = true;
            Destroyed?.Invoke(this, transform.position, info);
            _handle.Despawn();
        }
    }
}
