using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Plays a one-shot sprite-frame animation then returns itself to the pool. Generic —
    /// laser impacts now, enemy death puffs and muzzle flashes later. Never Destroyed (§4).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class PooledSpriteAnimator : MonoBehaviour, IPoolable
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(1f)] private float _fps = 24f;
        [Tooltip("Random extra rotation per play so repeated impacts don't look identical.")]
        [SerializeField] private bool _randomizeRotation = true;
        [Tooltip("Uniform scale jitter, ± this fraction.")]
        [SerializeField, Range(0f, 0.9f)] private float _scaleJitter = 0.2f;

        private SpriteRenderer _renderer;
        private PoolHandle _handle;
        private Vector3 _baseScale;
        private float _timer;
        private int _frame;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _handle = GetComponent<PoolHandle>();
            _baseScale = transform.localScale;
        }

        public void OnSpawned()
        {
            _timer = 0f;
            _frame = 0;

            if (_frames != null && _frames.Length > 0)
                _renderer.sprite = _frames[0];

            if (_randomizeRotation)
                transform.rotation = Quaternion.Euler(0f, 0f, Random.value * 360f);

            float s = 1f + Random.Range(-_scaleJitter, _scaleJitter);
            transform.localScale = _baseScale * s;
        }

        public void OnDespawned() { }

        private void Update()
        {
            if (_frames == null || _frames.Length == 0)
            {
                _handle.Despawn();
                return;
            }

            _timer += Time.deltaTime;
            int target = Mathf.FloorToInt(_timer * _fps);

            if (target >= _frames.Length)
            {
                _handle.Despawn();
                return;
            }

            if (target != _frame)
            {
                _frame = target;
                _renderer.sprite = _frames[_frame];
            }
        }
    }
}
