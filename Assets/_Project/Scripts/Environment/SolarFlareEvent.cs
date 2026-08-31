using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// M18 event — a solar flare. A warning strip glows on one edge, then a bright band
    /// sweeps across the whole arena, burning everything it passes (player and enemies —
    /// you have to be on the safe side of it). One sweep per event.
    /// </summary>
    public class SolarFlareEvent : SpaceEventBehaviour
    {
        [SerializeField] private GameObject _bandPrefab;      // wide bright strip, sprite only
        [SerializeField, Min(0.5f)] private float _warnTime = 2.5f;
        [SerializeField, Min(0.5f)] private float _sweepTime = 4f;
        [SerializeField, Min(0f)] private float _damagePerTick = 14f;
        [SerializeField, Min(0.05f)] private float _tickInterval = 0.25f;
        [Tooltip("Enemy + Player.")]
        [SerializeField] private LayerMask _targetLayers;
        [SerializeField, Min(1f)] private float _bandThickness = 2.2f;

        private Transform _band;
        private Vector2 _dir, _perp;
        private float _span, _nextTick;
        private static readonly Collider2D[] _hits = new Collider2D[64];

        protected override void OnBegin()
        {
            float a = Random.value * Mathf.PI * 2f;
            _dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            _perp = new Vector2(-_dir.y, _dir.x);

            var cam = Ctx.Camera;
            float h = cam != null ? cam.orthographicSize : 6f;
            float w = h * (cam != null ? cam.aspect : 1.78f);
            _span = Mathf.Sqrt(w * w + h * h) + 4f;

            var go = SpawnChild(_bandPrefab, StartPos, Quaternion.identity);
            _band = go != null ? go.transform : null;
            if (_band != null)
            {
                _band.right = _perp;
                _band.localScale = new Vector3(_span * 2f, _bandThickness, 1f);
            }
        }

        private Vector3 StartPos => PlayerPos - (Vector3)(_dir * _span);

        protected override void OnTick(float dt)
        {
            if (_band == null) return;

            if (Elapsed < _warnTime)
            {
                _band.position = StartPos;
                SetAlpha(Mathf.PingPong(Elapsed * 3f, 1f) * 0.5f + 0.15f);
                return;
            }

            float k = Mathf.Clamp01((Elapsed - _warnTime) / _sweepTime);
            _band.position = Vector3.Lerp(StartPos, PlayerPos + (Vector3)(_dir * _span), k);
            SetAlpha(k < 1f ? 0.9f : Mathf.Max(0f, 0.9f - (Elapsed - _warnTime - _sweepTime) * 1.5f));

            if (k <= 0f || k >= 1f || Elapsed < _nextTick) return;
            _nextTick = Elapsed + _tickInterval;

            float bandAngle = Vector2.SignedAngle(Vector2.right, _perp);
            int n = Physics2D.OverlapBoxNonAlloc(_band.position, new Vector2(_span * 2f, _bandThickness),
                bandAngle, _hits, _targetLayers);
            for (int i = 0; i < n; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                    target.TakeDamage(new DamageInfo(_damagePerTick, gameObject, col.transform.position, _dir));
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_band != null && _band.TryGetComponent(out SpriteRenderer sr))
            {
                var c = sr.color; c.a = Mathf.Clamp01(alpha); sr.color = c;
            }
        }
    }
}
