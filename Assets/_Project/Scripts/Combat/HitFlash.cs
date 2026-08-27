using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Briefly tints a SpriteRenderer toward a flash colour whenever its
    /// <see cref="HealthComponent"/> takes damage. Generic — the player ship now, enemies
    /// later (AI_Guidelines §1). Pool-safe: restores the base colour on despawn via the
    /// tint simply lerping back.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        [SerializeField] private Color _flashColor = new(1f, 0.35f, 0.35f);
        [SerializeField, Min(0.01f)] private float _duration = 0.09f;

        private SpriteRenderer _renderer;
        private Color _baseColor;
        private float _timer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseColor = _renderer.color;
            if (_health == null) _health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (_health != null) _health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_health != null) _health.Damaged -= HandleDamaged;
            if (_renderer != null) _renderer.color = _baseColor;
        }

        private void HandleDamaged(DamageInfo _) => _timer = _duration;

        private void Update()
        {
            if (_timer > 0f)
            {
                _timer -= Time.deltaTime;
                _renderer.color = Color.Lerp(_baseColor, _flashColor, Mathf.Clamp01(_timer / _duration));
            }
            else if (_renderer.color != _baseColor)
            {
                _renderer.color = _baseColor;
            }
        }
    }
}
