using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Makes the main-menu camera a slow-moving window on a patch of space (M20). It drifts along
    /// a lazy Lissajous path and leans a little toward the mouse, so the shared
    /// <see cref="StarfieldParallax"/> behind it is always alive, and it feeds the player's
    /// currently-selected map nebula into that starfield. Menu-only cosmetic — no gameplay
    /// (AI_Guidelines §1). Built + wired by <c>MainMenuBuilder</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuDiorama : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private StarfieldParallax _starfield;

        [Tooltip("Used when no map is selected / the catalogue is empty.")]
        [SerializeField] private Sprite _fallbackNebula;
        [SerializeField] private Color _fallbackNebulaTint = new(0.62f, 0.72f, 1f, 0.5f);
        [SerializeField] private Color _fallbackStarTint = new(0.7f, 0.78f, 1f, 1f);

        [SerializeField] private Vector2 _driftAmplitude = new(1.35f, 0.8f);
        [SerializeField] private Vector2 _driftFrequency = new(0.045f, 0.033f);
        [SerializeField, Range(0f, 3f)] private float _mouseLean = 0.6f;
        [SerializeField, Min(0.1f)] private float _mouseEase = 2.4f;

        private Vector3 _home;
        private Vector2 _mouseOffset;
        private float _t;

        private void Awake()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
            _home = _camera != null ? _camera.transform.position : Vector3.zero;
        }

        // Start, not Awake — the StarfieldParallax has to build its layers first (SetTint no-ops
        // until it has).
        private void Start() => ApplyNebula();

        private void ApplyNebula()
        {
            if (_starfield == null) return;

            Sprite nebula = _fallbackNebula;
            Color nebulaTint = _fallbackNebulaTint;
            Color starTint = _fallbackStarTint;

            var map = MapService.Selected;
            if (map != null && map.backdropSprite != null)
            {
                nebula = map.backdropSprite;
                nebulaTint = map.backdropTint;
                starTint = map.starfieldTint;
                if (_camera != null) _camera.backgroundColor = map.cameraBackground;
            }

            _starfield.SetTint(starTint);
            _starfield.SetBackdrop(nebula, nebulaTint);
        }

        private void Update()
        {
            if (_camera == null) return;
            _t += Time.unscaledDeltaTime;

            Vector2 drift = new(
                Mathf.Sin(_t * _driftFrequency.x * Mathf.PI * 2f) * _driftAmplitude.x,
                Mathf.Sin(_t * _driftFrequency.y * Mathf.PI * 2f) * _driftAmplitude.y);

            Vector2 target = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null && _mouseLean > 0f)
            {
                Vector2 vp = _camera.ScreenToViewportPoint(mouse.position.ReadValue());
                target = (vp - new Vector2(0.5f, 0.5f)) * (2f * _mouseLean);
            }
            _mouseOffset = Vector2.Lerp(_mouseOffset, target,
                1f - Mathf.Exp(-_mouseEase * Time.unscaledDeltaTime));

            _camera.transform.position = _home + (Vector3)(drift + _mouseOffset);
        }
    }
}
