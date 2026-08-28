using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Infinite scrolling parallax starfield for the open arena. Builds a few tiled
    /// <see cref="SpriteRenderer"/> layers (one seamless star texture) and, each frame,
    /// slides each layer by the camera position times a per-layer parallax factor, wrapped
    /// to the tile size so it repeats forever with no visible seam.
    ///
    /// Pure background view glue — no gameplay, lives in Core (AI_Guidelines §1). Drop it on
    /// an empty GameObject; it parents its layers under the camera automatically.
    /// </summary>
    [DisallowMultipleComponent]
    public class StarfieldParallax : MonoBehaviour
    {
        [System.Serializable]
        public struct Layer
        {
            [Tooltip("0 = fixed (infinitely far), 1 = moves 1:1 with the camera.")]
            [Range(0f, 1f)] public float parallax;
            [Tooltip("Brightness multiplier for this layer.")]
            [Range(0f, 1f)] public float brightness;
            [Tooltip("Tiling density multiplier (higher = smaller, denser stars).")]
            [Min(0.05f)] public float density;
        }

        [SerializeField] private Camera _camera;
        [SerializeField] private Sprite _starSprite;
        [SerializeField] private Color _tint = new Color(0.75f, 0.82f, 1f, 1f);

        [Tooltip("How many screen-widths each layer quad spans. Must exceed 1 + wrap slack.")]
        [SerializeField, Min(1.5f)] private float _coverage = 3f;

        [Tooltip("Sorting order of the nearest layer; layers behind it step down by 1.")]
        [SerializeField] private int _baseSortingOrder = -100;

        [SerializeField]
        private Layer[] _layers =
        {
            new Layer { parallax = 0.03f, brightness = 0.30f, density = 0.55f },
            new Layer { parallax = 0.08f, brightness = 0.50f, density = 0.85f },
            new Layer { parallax = 0.16f, brightness = 0.72f, density = 1.15f },
        };

        private Transform[] _layerTf;
        private float[] _tileSize;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null || _starSprite == null)
            {
                Debug.LogError($"{nameof(StarfieldParallax)} on '{name}' needs a camera and a star sprite.", this);
                enabled = false;
                return;
            }
            BuildLayers();
        }

        private void BuildLayers()
        {
            float viewH = _camera.orthographicSize * 2f;
            float viewW = viewH * _camera.aspect;
            float spanW = viewW * _coverage;
            float spanH = viewH * _coverage;
            float spriteWorld = _starSprite.rect.width / _starSprite.pixelsPerUnit;

            _layerTf = new Transform[_layers.Length];
            _tileSize = new float[_layers.Length];

            for (int i = 0; i < _layers.Length; i++)
            {
                float density = Mathf.Max(0.05f, _layers[i].density);

                var go = new GameObject($"StarLayer_{i}");
                go.transform.SetParent(_camera.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 20f + i);
                go.transform.localScale = new Vector3(1f / density, 1f / density, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _starSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(spanW * density, spanH * density);

                Color c = _tint * _layers[i].brightness;
                sr.color = new Color(c.r, c.g, c.b, 1f);
                sr.sortingOrder = _baseSortingOrder - (_layers.Length - 1 - i);

                _layerTf[i] = go.transform;
                _tileSize[i] = spriteWorld / density;
            }
        }

        private void LateUpdate()
        {
            if (_layerTf == null) return;

            Vector3 cam = _camera.transform.position;
            for (int i = 0; i < _layerTf.Length; i++)
            {
                float t = _tileSize[i];
                float ox = -Mathf.Repeat(cam.x * _layers[i].parallax, t);
                float oy = -Mathf.Repeat(cam.y * _layers[i].parallax, t);
                var lp = _layerTf[i].localPosition;
                _layerTf[i].localPosition = new Vector3(ox, oy, lp.z);
            }
        }
    }
}
