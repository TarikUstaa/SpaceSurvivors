using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Visible armor plating: every <see cref="_hitsPerBlock"/>th incoming hit bounces off
    /// entirely instead of costing HP. Reads as "some of my hits just don't count" — easier
    /// to notice and react to than a flat damage reduction would be. Composes via
    /// <see cref="IDamageInterceptor"/> exactly like the player's own
    /// <see cref="ShieldComponent"/>, but with no StatSheet dependency — enemies don't have
    /// one (AI_Guidelines §1, §2).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArmorPlating : MonoBehaviour, IDamageInterceptor, IPoolable
    {
        [Tooltip("Every Nth hit is fully absorbed. 3 = every third hit bounces off.")]
        [SerializeField, Min(2)] private int _hitsPerBlock = 3;

        [Tooltip("Optional. Empty = auto-resolve a SpriteRenderer on this GameObject or its children.")]
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _blockFlash = new(0.65f, 0.88f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.12f;

        private int _hitsTaken;
        private Color _baseColor = Color.white;
        private float _flashTimer;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        public void OnSpawned() => _hitsTaken = 0;
        public void OnDespawned() { }

        /// <inheritdoc />
        public bool Intercept(in DamageInfo info)
        {
            _hitsTaken++;
            if (_hitsTaken % _hitsPerBlock != 0) return false;

            _flashTimer = _flashDuration;
            return true;
        }

        private void Update()
        {
            if (_flashTimer <= 0f || _renderer == null) return;

            _flashTimer -= Time.deltaTime;
            _renderer.color = _flashTimer > 0f ? _blockFlash : _baseColor;
        }
    }
}
