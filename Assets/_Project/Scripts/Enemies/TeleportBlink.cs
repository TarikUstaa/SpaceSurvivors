using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Every <see cref="_interval"/> seconds, blinks to a new point roughly
    /// <see cref="_blinkDistance"/> from the target instead of closing the distance on foot —
    /// breaks the read a player builds up while kiting a normal chaser. Only ever touches
    /// position, never velocity, so it layers on top of whatever <see cref="IMoveStrategy"/>
    /// the enemy already has the same way <c>SeparationSteering</c> layers onto everything
    /// else (AI_Guidelines §1) — it doesn't replace the base move strategy, it interrupts it
    /// periodically.
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class TeleportBlink : MonoBehaviour, IPoolable
    {
        [Tooltip("Seconds between blinks.")]
        [SerializeField, Min(0.5f)] private float _interval = 3.5f;
        [Tooltip("Distance from the target the blink lands at.")]
        [SerializeField, Min(0.5f)] private float _blinkDistance = 3.5f;
        [Tooltip("Optional pooled one-shot VFX played at both the departure and arrival point.")]
        [SerializeField] private GameObject _blinkVfxPrefab;

        private EnemyBrain _brain;
        private Rigidbody2D _body;
        private PoolManager _pool;
        private float _timer;

        private void Awake()
        {
            _brain = GetComponent<EnemyBrain>();
            _body = GetComponent<Rigidbody2D>();
            _pool = FindAnyObjectByType<PoolManager>();
        }

        public void OnSpawned() => _timer = _interval;
        public void OnDespawned() { }

        private void Update()
        {
            if (_brain == null || !_brain.IsActive || _brain.Target == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _interval;
            Blink();
        }

        private void Blink()
        {
            SpawnVfx(_body.position);

            Vector2 targetPos = _brain.Target.position;
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            Vector2 landing = targetPos + dir.normalized * _blinkDistance;

            _body.position = landing;
            transform.position = landing;

            SpawnVfx(landing);
        }

        private void SpawnVfx(Vector2 at)
        {
            if (_blinkVfxPrefab != null && _pool != null)
                _pool.Spawn(_blinkVfxPrefab, at, Quaternion.identity);
        }
    }
}
