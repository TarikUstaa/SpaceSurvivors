using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Full-screen red edge flash when the player is hit, plus a steady low-HP pulse.
    /// Builds its own overlay canvas (bootstrap UI, see <see cref="UiBuilder"/>); reacts to
    /// the player's <see cref="HealthComponent"/> events and owns no game state
    /// (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField] private HealthComponent _playerHealth;
        [SerializeField] private Sprite _vignetteSprite;
        [SerializeField] private Color _color = new(1f, 0.1f, 0.1f);
        [SerializeField, Range(0f, 1f)] private float _hitAlpha = 0.6f;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 3f;
        [Tooltip("Below this HP fraction a constant pulsing vignette kicks in.")]
        [SerializeField, Range(0f, 1f)] private float _lowHpThreshold = 0.3f;

        private Image _image;
        private float _hitLevel;

        private void Awake()
        {
            if (_playerHealth == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerHealth = player.GetComponent<HealthComponent>();
                if (_playerHealth == null) _playerHealth = FindFirstObjectByType<HealthComponent>();
            }
            if (_vignetteSprite == null)
                _vignetteSprite = Resources.Load<Sprite>("DamageVignette");

            BuildUI();
        }

        private void OnEnable()
        {
            if (_playerHealth != null) _playerHealth.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_playerHealth != null) _playerHealth.Damaged -= HandleDamaged;
        }

        private void HandleDamaged(DamageInfo _) => _hitLevel = 1f;

        private void Update()
        {
            _hitLevel = Mathf.MoveTowards(_hitLevel, 0f, _fadeSpeed * Time.unscaledDeltaTime);

            float lowHp = 0f;
            if (_playerHealth != null && _playerHealth.IsAlive && _playerHealth.Normalized < _lowHpThreshold)
            {
                float t = 1f - _playerHealth.Normalized / Mathf.Max(0.001f, _lowHpThreshold);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                lowHp = Mathf.Lerp(0.12f, 0.4f, t) * Mathf.Lerp(0.6f, 1f, pulse);
            }

            float alpha = Mathf.Max(_hitLevel * _hitAlpha, lowHp);
            var c = _color;
            c.a = alpha;
            _image.color = c;
        }

        private void BuildUI()
        {
            var canvas = UiBuilder.Canvas("DamageVignetteCanvas", transform, 200);
            _image = UiBuilder.Image("Vignette", canvas.transform, new Color(_color.r, _color.g, _color.b, 0f));
            UiBuilder.Stretch(_image.rectTransform);
            _image.sprite = _vignetteSprite;
            _image.type = Image.Type.Simple;
            _image.raycastTarget = false;
            _image.preserveAspect = false;
        }
    }
}
