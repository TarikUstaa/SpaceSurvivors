using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Shared behaviour for bonus drops (health capsules, power-ups): idle bob/spin, then
    /// fly to the ship once inside the <see cref="ScrapCollector"/> magnet range, and
    /// <see cref="Collect"/> on contact. Subclasses only implement the effect
    /// (<see cref="OnCollected"/>). Pooled, never Destroyed (AI_Guidelines §1, §4).
    ///
    /// <see cref="XpPickup"/> keeps its own copy of this loop on purpose — it predates this
    /// base and its Absorb path is bespoke; don't fold it in just to share code.
    /// </summary>
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public abstract class FlyToPlayerPickup : MonoBehaviour, IPoolable, ICollectible
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _flyMaxSpeed = 13f;
        [SerializeField, Min(0f)] private float _flyAcceleration = 48f;
        [SerializeField] private float _bobAmplitude = 0.08f;
        [SerializeField] private float _bobSpeed = 3.5f;
        [SerializeField] private float _spinSpeed = 40f;

        private PoolHandle _handle;
        private bool _flying;
        private float _speed;
        private Vector3 _restPos;
        private float _bobPhase;

        /// <summary>The player's collector — resolved on spawn (or injected via <see cref="Bind"/>).</summary>
        protected ScrapCollector Collector { get; private set; }

        private void Awake() => _handle = GetComponent<PoolHandle>();

        /// <summary>Optional: the dropper can hand us the collector so we skip the Find.</summary>
        public void Bind(ScrapCollector collector) => Collector = collector;

        public virtual void OnSpawned()
        {
            if (Collector == null) Collector = FindFirstObjectByType<ScrapCollector>();
            _flying = false;
            _speed = 0f;
            _restPos = transform.position;
            _bobPhase = Random.value * Mathf.PI * 2f;
        }

        public virtual void OnDespawned() => _flying = false;

        private void Update()
        {
            if (Collector == null) return;

            Vector2 self = transform.position;
            Vector2 target = Collector.transform.position;
            float dist = Vector2.Distance(self, target);

            if (dist <= Collector.CollectRadius)
            {
                Collect(Collector.gameObject);
                return;
            }

            if (!_flying && dist <= Collector.MagnetRadius)
                _flying = true;

            if (_flying)
            {
                _speed = Mathf.MoveTowards(_speed, _flyMaxSpeed, _flyAcceleration * Time.deltaTime);
                transform.position = self + (target - self).normalized * (_speed * Time.deltaTime);
            }
            else
            {
                if (_bobAmplitude > 0f)
                {
                    _bobPhase += _bobSpeed * Time.deltaTime;
                    transform.position = _restPos + Vector3.up * (Mathf.Sin(_bobPhase) * _bobAmplitude);
                }
                if (_spinSpeed != 0f) transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
            }
        }

        public void Collect(GameObject collector)
        {
            OnCollected(collector);
            _handle.Despawn();
        }

        /// <summary>Apply the pickup's effect to <paramref name="collector"/> (the player).</summary>
        protected abstract void OnCollected(GameObject collector);
    }
}
