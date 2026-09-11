using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// M18 event — the arena fills with static. A crackle overlay follows the camera and,
    /// every <see cref="_pulseInterval"/> seconds, an EMP wave zaps every enemy on the Enemy
    /// layer within a wide radius of the player. Player-favourable (a brief reprieve), so the
    /// per-pulse damage is modest.
    /// </summary>
    public class IonStormEvent : SpaceEventBehaviour
    {
        [SerializeField] private GameObject _overlayPrefab;
        [SerializeField] private GameObject _zapVfxPrefab;
        [SerializeField, Min(0.2f)] private float _pulseInterval = 1.8f;
        [SerializeField, Min(1f)] private float _pulseRadius = 11f;
        [SerializeField, Min(0f)] private float _pulseDamage = 6f;
        [SerializeField] private LayerMask _enemyLayers;

        private Transform _overlay;
        private float _next;
        private static readonly Collider2D[] _hits = new Collider2D[64];

        protected override void OnBegin()
        {
            _next = 1f;
            var go = SpawnChild(_overlayPrefab, PlayerPos, Quaternion.identity);
            _overlay = go != null ? go.transform : null;
        }

        protected override void OnTick(float dt)
        {
            if (_overlay != null && Ctx.Camera != null)
                _overlay.position = new Vector3(Ctx.Camera.transform.position.x, Ctx.Camera.transform.position.y, _overlay.position.z);

            if (Elapsed < _next) return;
            _next = Elapsed + _pulseInterval;

            int n = Physics2D.OverlapCircle(PlayerPos, _pulseRadius,
                OverlapFilter.For(_enemyLayers), _hits);
            for (int i = 0; i < n; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive) continue;
                target.TakeDamage(new DamageInfo(_pulseDamage, gameObject, col.transform.position, Vector2.zero));
                // One-shot VFX cleans itself up — spawn it loose, don't track it for release.
                if (_zapVfxPrefab != null && Ctx.Pool != null)
                    Ctx.Pool.Spawn(_zapVfxPrefab, col.transform.position, Quaternion.identity);
            }
        }
    }
}
