using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Visual for the <see cref="ShieldComponent"/>: a bubble around the ship that is
    /// visible while any charge remains, pops on absorb, and — as the player stacks the
    /// Shield upgrade — <b>steps up through stronger sprites and grows</b>
    /// (<see cref="_tierSprites"/>[0..] = 1 stack, 2 stacks, 3+). Pure view (AI_Guidelines §1).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class ShieldView : MonoBehaviour
    {
        [SerializeField] private ShieldComponent _shield;

        [Tooltip("One sprite per shield tier: element 0 = 1 charge max, 1 = 2, 2 = 3+ ...")]
        [SerializeField] private Sprite[] _tierSprites;

        [SerializeField] private Color _color = new(0.4f, 0.8f, 1f, 0.35f);
        [Tooltip("Extra uniform scale added per tier above the first.")]
        [SerializeField, Min(0f)] private float _scalePerTier = 0.18f;
        [SerializeField, Min(0f)] private float _flashDuration = 0.18f;
        [SerializeField, Min(1f)] private float _flashScale = 1.35f;

        private SpriteRenderer _renderer;
        private Vector3 _designScale;
        private int _current;
        private int _max;
        private float _flash;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _designScale = transform.localScale;
            if (_shield == null) _shield = GetComponentInParent<ShieldComponent>();
        }

        private void OnEnable()
        {
            if (_shield == null) return;
            _shield.ShieldChanged += HandleChanged;
            _shield.Absorbed += HandleAbsorbed;
            HandleChanged(_shield.CurrentCharges, _shield.MaxCharges);
        }

        private void OnDisable()
        {
            if (_shield == null) return;
            _shield.ShieldChanged -= HandleChanged;
            _shield.Absorbed -= HandleAbsorbed;
        }

        private void HandleChanged(int current, int max)
        {
            if (max > _max && _max > 0) _flash = _flashDuration; // "power up" pop on upgrade
            _current = current;
            _max = max;

            if (_tierSprites is { Length: > 0 })
            {
                int tier = Mathf.Clamp(max - 1, 0, _tierSprites.Length - 1);
                if (_tierSprites[tier] != null) _renderer.sprite = _tierSprites[tier];
            }
        }

        private void HandleAbsorbed() => _flash = _flashDuration;

        private void Update()
        {
            bool up = _current > 0;
            _renderer.enabled = up || _flash > 0f;
            if (!_renderer.enabled) return;

            float chargeT = _max > 0 ? (float)_current / _max : 0f;
            float baseAlpha = _color.a * Mathf.Lerp(0.5f, 1f, chargeT);

            if (_flash > 0f) _flash -= Time.deltaTime;
            float flashT = Mathf.Clamp01(_flash / Mathf.Max(0.0001f, _flashDuration));

            var c = _color;
            c.a = Mathf.Lerp(baseAlpha, 0.9f, flashT);
            _renderer.color = c;

            float tierScale = 1f + Mathf.Max(0, _max - 1) * _scalePerTier;
            transform.localScale = _designScale * (tierScale * Mathf.Lerp(1f, _flashScale, flashT));
        }
    }
}
