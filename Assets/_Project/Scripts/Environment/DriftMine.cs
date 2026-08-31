using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// A neutral hazard that drifts through the arena (M18). It arms shortly after spawn, then
    /// detonates on the first thing that touches it — player <b>or</b> enemy — with an
    /// <see cref="Aoe.Splash"/> that hits everyone nearby. Pooled, streamed like any other prop
    /// by the <see cref="EnvironmentDirector"/>. Trigger collider — you can fly into it, at your
    /// own risk; a red halo + hard blink + slow tumble telegraph the danger.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class DriftMine : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0f)] private float _driftSpeed = 0.7f;
        [SerializeField, Min(0f)] private float _armDelay = 0.6f;
        [SerializeField, Min(0f)] private float _blastRadius = 2.6f;
        [SerializeField, Min(0f)] private float _blastDamage = 40f;
        [SerializeField] private GameObject _blastVfxPrefab;
        [Tooltip("Optional child that blinks faster as the mine stays live.")]
        [SerializeField] private SpriteRenderer _light;

        private Rigidbody2D _body;
        private PoolHandle _handle;
        private PoolManager _pool;
        private Vector2 _drift;
        private float _age;
        private bool _spent;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _handle = GetComponent<PoolHandle>();
            _pool = FindFirstObjectByType<PoolManager>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void OnSpawned()
        {
            _age = 0f;
            _spent = false;
            float a = Random.value * Mathf.PI * 2f;
            _drift = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _driftSpeed;
            _body.linearVelocity = Vector2.zero;
        }

        public void OnDespawned() => _drift = Vector2.zero;

        private void FixedUpdate()
        {
            if (_drift != Vector2.zero) _body.MovePosition(_body.position + _drift * Time.fixedDeltaTime);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            // Slow tumble so it never blends into the static rocks.
            transform.Rotate(0f, 0f, 35f * Time.deltaTime);

            if (_light != null)
            {
                // A hard on/off blink, speeding up the longer it's live.
                float rate = Mathf.Lerp(1.6f, 6f, Mathf.Clamp01(_age / 8f));
                float blink = Mathf.Sin(_age * rate * Mathf.PI * 2f) > 0f ? 1f : 0.12f;
                var c = _light.color; c.a = blink; _light.color = c;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_spent || _age < _armDelay) return;
            if (other.GetComponentInParent<IDamageable>() == null) return; // only living things trigger it
            Detonate();
        }

        private void Detonate()
        {
            _spent = true;
            Aoe.Splash(transform.position, _blastRadius, _blastDamage, gameObject);
            if (_blastVfxPrefab != null && _pool != null)
                _pool.Spawn(_blastVfxPrefab, transform.position, Quaternion.identity);
            _handle.Despawn();
        }
    }
}
