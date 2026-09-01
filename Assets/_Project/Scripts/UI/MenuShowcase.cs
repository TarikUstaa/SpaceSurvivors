using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// The small diorama in front of the main-menu camera (M20): the player's currently-selected
    /// ship idling with a gentle bob + sway, and a handful of asteroids drifting past and
    /// wrapping around when they leave the frame. Pure decoration — no pooling, physics or
    /// gameplay (AI_Guidelines §1). Built + wired by <c>MainMenuBuilder</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuShowcase : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ship;
        [SerializeField] private Transform _shipEngineGlow;
        [SerializeField] private SpriteRenderer[] _rocks;

        [SerializeField] private float _wrapRadius = 10f;
        [Tooltip("Rocks stay outside this radius so they never drift over the centred UI " +
                 "(which fills a tall strip down the middle, not just a disc).")]
        [SerializeField] private float _keepOutRadius = 8.5f;
        [SerializeField] private Vector2 _rockSpeedRange = new(0.3f, 0.75f);
        [SerializeField] private float _bobDistance = 0.22f;
        [SerializeField] private float _swayDegrees = 6f;

        private Vector2[] _rockVelocity;
        private float _rockSpin_a;
        private Vector3 _shipHome;
        private Vector3 _glowHome;
        private float _t;

        private void Awake()
        {
            if (_ship != null)
            {
                _shipHome = _ship.transform.localPosition;
                var selected = ShipService.SelectedSprite;
                if (selected != null) _ship.sprite = selected;
            }
            if (_shipEngineGlow != null) _glowHome = _shipEngineGlow.localScale;

            int n = _rocks != null ? _rocks.Length : 0;
            _rockVelocity = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var t = _rocks[i].transform;
                // Nudge any rock that starts too near the centre back out into the ring.
                Vector2 p = t.localPosition;
                if (p.magnitude < _keepOutRadius)
                    t.localPosition = (Vector3)(p.normalized * (_keepOutRadius + 1.5f))
                                      + new Vector3(0f, 0f, t.localPosition.z);

                float a = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(_rockSpeedRange.x, _rockSpeedRange.y);
                _rockVelocity[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
                t.localRotation = Quaternion.Euler(0f, 0f, Random.value * 360f);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;

            if (_ship != null)
            {
                _ship.transform.localPosition = _shipHome + new Vector3(
                    Mathf.Sin(_t * 0.6f) * _bobDistance,
                    Mathf.Sin(_t * 0.9f) * (_bobDistance * 0.7f), 0f);
                _ship.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 0.5f) * _swayDegrees);
            }

            if (_shipEngineGlow != null)
            {
                float pulse = 1f + Mathf.Sin(_t * 6f) * 0.18f;
                _shipEngineGlow.localScale = _glowHome * pulse;
            }

            for (int i = 0; i < _rockVelocity.Length; i++)
            {
                var t = _rocks[i].transform;
                t.localPosition += (Vector3)(_rockVelocity[i] * dt);
                t.localRotation *= Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 10f : -8f) * dt);

                Vector2 p = t.localPosition;
                float r = p.magnitude;
                if (r > _wrapRadius)
                {
                    p = -p.normalized * (_wrapRadius * 0.95f);
                    t.localPosition = new Vector3(p.x, p.y, t.localPosition.z);
                }
                else if (r < _keepOutRadius && r > 0.01f)
                {
                    // bounce off the inner keep-out ring so rocks orbit the edges, never the UI
                    Vector2 nrm = p / r;
                    _rockVelocity[i] = Vector2.Reflect(_rockVelocity[i], nrm);
                    p = nrm * _keepOutRadius;
                    t.localPosition = new Vector3(p.x, p.y, t.localPosition.z);
                }
            }
        }
    }
}
