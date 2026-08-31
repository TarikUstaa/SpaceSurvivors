using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Pooled scrap / XP drop. Sits still until the player's <see cref="ScrapCollector"/>
    /// magnet range reaches it, then accelerates toward the ship and is absorbed on contact.
    ///
    /// * <see cref="IPoolable"/> — reset every spawn, never Destroyed (AI_Guidelines §4).
    /// * <see cref="ICollectible"/> — the actual pickup action, callable by anything (§2).
    /// * Self-detects the player by distance (few pickups on screen) instead of a physics
    ///   trigger or a scan from the collector.
    /// </summary>
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class XpPickup : MonoBehaviour, IPoolable, ICollectible
    {
        [SerializeField, Min(0f)] private float _flyMaxSpeed = 14f;
        [SerializeField, Min(0f)] private float _flyAcceleration = 55f;
        [Tooltip("Little idle bob so drops read as pickups, not scenery.")]
        [SerializeField] private float _bobAmplitude = 0.06f;
        [SerializeField] private float _bobSpeed = 4f;
        [Tooltip("Idle spin (deg/s). 0 = none. A slow spin marks a special boss orb.")]
        [SerializeField] private float _spinSpeed = 0f;

        [Header("Boss orb (optional)")]
        [Tooltip("On pickup, grant this many guaranteed full level-ups on top of the XP value.")]
        [SerializeField, Min(0)] private int _guaranteedLevelUps = 0;

        private PoolHandle _handle;
        private ScrapCollector _collector;
        private int _scrap;
        private int _xp;
        private bool _flying;
        private float _speed;
        private Vector3 _restPos;
        private float _bobPhase;

        private void Awake() => _handle = GetComponent<PoolHandle>();

        /// <summary>Called by <see cref="LootDropper"/> right after the pool spawns this.</summary>
        public void Configure(ScrapCollector collector, int scrapValue, int xpValue)
        {
            _collector = collector;
            _scrap = scrapValue;
            _xp = xpValue;
        }

        /// <summary>Fly to the ship now, ignoring magnet range — used by the Magnet pickup.</summary>
        public void Attract() => _flying = true;

        public void OnSpawned()
        {
            _flying = false;
            _speed = 0f;
            _restPos = transform.position;
            _bobPhase = Random.value * Mathf.PI * 2f;
            transform.rotation = Quaternion.identity;
        }

        public void OnDespawned()
        {
            _collector = null;
            _flying = false;
        }

        private void Update()
        {
            if (_collector == null) return;

            Vector2 self = transform.position;
            Vector2 target = _collector.transform.position;
            float dist = Vector2.Distance(self, target);

            if (dist <= _collector.CollectRadius)
            {
                Collect(_collector.gameObject);
                return;
            }

            if (!_flying && dist <= _collector.MagnetRadius)
                _flying = true;

            if (_flying)
            {
                _speed = Mathf.MoveTowards(_speed, _flyMaxSpeed, _flyAcceleration * Time.deltaTime);
                Vector2 dir = (target - self).normalized;
                transform.position = self + dir * (_speed * Time.deltaTime);
            }
            else if (_bobAmplitude > 0f)
            {
                _bobPhase += _bobSpeed * Time.deltaTime;
                transform.position = _restPos + Vector3.up * (Mathf.Sin(_bobPhase) * _bobAmplitude);
            }

            if (_spinSpeed != 0f && !_flying)
                transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
        }

        public void Collect(GameObject collector)
        {
            if (collector != null && collector.TryGetComponent(out ScrapCollector sc))
                sc.Absorb(_scrap, _xp, _guaranteedLevelUps);
            else
                _collector?.Absorb(_scrap, _xp, _guaranteedLevelUps);

            _handle.Despawn();
        }
    }
}
