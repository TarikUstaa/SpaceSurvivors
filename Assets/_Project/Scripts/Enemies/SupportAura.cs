using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Speeds up every other enemy within <see cref="_radius"/>, refreshed on a short pulse
    /// while this one is alive — kill the support unit first to strip the buff off the swarm
    /// around it, the classic "protect the healer" incentive. Only ever reads sibling
    /// <see cref="EnemyBrain"/>s and applies their own public buff API — never touches the
    /// player (AI_Guidelines §1, §2).
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    [DisallowMultipleComponent]
    public class SupportAura : MonoBehaviour, IPoolable
    {
        [Tooltip("Enemies within this range get the speed buff.")]
        [SerializeField, Min(1f)] private float _radius = 5f;
        [Tooltip("Speed multiplier applied to buffed enemies.")]
        [SerializeField, Min(1f)] private float _speedMultiplier = 1.3f;
        [Tooltip("Seconds between pulses. The buff itself outlasts one pulse (1.5x this) so a " +
                 "brief gap in range doesn't instantly drop it.")]
        [SerializeField, Min(0.1f)] private float _pulseInterval = 0.5f;
        [SerializeField] private LayerMask _enemyLayers;

        private static readonly Collider2D[] Buffer = new Collider2D[32];

        private EnemyBrain _self;
        private float _timer;

        private void Awake() => _self = GetComponent<EnemyBrain>();

        public void OnSpawned() => _timer = 0f;
        public void OnDespawned() { }

        private void Update()
        {
            if (_self == null || !_self.IsActive) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _pulseInterval;
            Pulse();
        }

        private void Pulse()
        {
            int n = Physics2D.OverlapCircleNonAlloc(transform.position, _radius, Buffer, _enemyLayers);
            for (int i = 0; i < n; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;

                var brain = col.GetComponentInParent<EnemyBrain>();
                if (brain == null || brain == _self || !brain.IsActive) continue;

                brain.ApplySpeedBuff(_speedMultiplier, _pulseInterval * 1.5f);
            }
        }
    }
}
