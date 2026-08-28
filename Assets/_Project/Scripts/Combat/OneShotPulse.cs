using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// A pooled one-shot flourish: a sprite that scales out and fades over
    /// <see cref="_lifetime"/> seconds, then returns to the pool. Generic — heal bursts,
    /// power-up flashes, pickup pops (AI_Guidelines §1, §4). Never Destroyed.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class OneShotPulse : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.05f)] private float _lifetime = 0.4f;
        [SerializeField, Min(0f)] private float _startScale = 0.4f;
        [SerializeField, Min(0f)] private float _endScale = 2.2f;
        [SerializeField] private AnimationCurve _alphaOverLife = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private float _spinDegrees = 0f;

        private SpriteRenderer _renderer;
        private PoolHandle _handle;
        private Color _baseColor;
        private float _t;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _handle = GetComponent<PoolHandle>();
            _baseColor = _renderer.color;
        }

        public void OnSpawned()
        {
            _t = 0f;
            transform.localScale = Vector3.one * _startScale;
            _renderer.color = _baseColor;
        }

        public void OnDespawned() { }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = _t / _lifetime;
            if (k >= 1f) { _handle.Despawn(); return; }

            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, k);
            if (_spinDegrees != 0f) transform.Rotate(0f, 0f, _spinDegrees * Time.deltaTime);

            var c = _baseColor;
            c.a = _baseColor.a * Mathf.Clamp01(_alphaOverLife.Evaluate(k));
            _renderer.color = c;
        }
    }
}
