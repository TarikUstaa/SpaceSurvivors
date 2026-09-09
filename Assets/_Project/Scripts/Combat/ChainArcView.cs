using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// The visible bolt for a <see cref="ChainLightning"/> cast: one <see cref="LineRenderer"/>
    /// polyline through the ship and every struck enemy, snapped on at full brightness and
    /// faded out over <see cref="FadeSeconds"/> seconds. One instance per weapon rig, reused every cast —
    /// no allocation, no pooling machinery, and nothing left on screen between shots.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChainArcView : MonoBehaviour
    {
        private const float FadeSeconds = 0.14f;
        private const float Width = 0.09f;

        private LineRenderer _line;
        private Color _colour = new(0.6f, 0.85f, 1f, 1f);
        private float _age = FadeSeconds;   // starts "done" so nothing shows until the first Flash

        private void Awake()
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.textureMode = LineTextureMode.Stretch;
            _line.numCornerVertices = 2;
            _line.numCapVertices = 2;
            _line.widthCurve = AnimationCurve.Linear(0f, Width, 1f, Width * 0.4f);
            _line.sortingOrder = 20;
            _line.enabled = false;
        }

        public void SetColour(Color c) => _colour = c;

        /// <summary>Draw the bolt through <paramref name="nodes"/> (ship first, then each target) and start it fading.</summary>
        public void Flash(IReadOnlyList<Vector3> nodes)
        {
            if (nodes == null || nodes.Count < 2) return;

            _line.positionCount = nodes.Count;
            for (int i = 0; i < nodes.Count; i++) _line.SetPosition(i, nodes[i]);

            _age = 0f;
            _line.enabled = true;
            Paint(1f);
        }

        private void Update()
        {
            if (!_line.enabled) return;

            _age += Time.deltaTime;
            if (_age >= FadeSeconds)
            {
                _line.enabled = false;
                return;
            }
            Paint(1f - _age / FadeSeconds);
        }

        private void Paint(float alpha)
        {
            var c = _colour;
            c.a *= alpha;
            // A hot white core that cools to the tint as it fades.
            Color core = Color.Lerp(c, Color.white, 0.5f * alpha);
            _line.startColor = core;
            _line.endColor = c;
        }
    }
}
