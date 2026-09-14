using SpaceSurvivors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// A pooled, world-space floating number: rises, fades, and returns itself to the pool.
    /// Spawned by <see cref="DamageNumberSpawner"/> at the point of every landed hit.
    ///
    /// <para>Builds its own <see cref="Canvas"/>/<see cref="Text"/> visual in <see cref="Awake"/>
    /// — the same idea as <see cref="ChainArcView"/> building its own <c>LineRenderer</c> —
    /// so the pooled prefab itself stays a single, trivial GameObject with just this script
    /// on it. Nothing in the scene needs to know how a floating number is drawn
    /// (AI_Guidelines §1, §4).</para>
    /// </summary>
    [RequireComponent(typeof(PoolHandle))]
    [DisallowMultipleComponent]
    public class DamageNumber : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.05f)] private float _lifetime = 0.6f;
        [SerializeField] private float _riseDistance = 0.7f;
        [SerializeField] private int _fontSize = 28;
        [SerializeField] private Color _color = Color.white;

        /// <summary>Canvas is in screen-space pixels; this scales it down to a readable size in world units.</summary>
        private const float WorldScale = 0.012f;

        /// <summary>Above every gameplay sprite and above <see cref="ChainArcView"/>'s bolt (20).</summary>
        private const int SortingOrder = 25;

        private PoolHandle _handle;
        private Text _text;
        private Vector3 _start;
        private float _age;

        private void Awake()
        {
            _handle = GetComponent<PoolHandle>();

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localScale = Vector3.one * WorldScale;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = SortingOrder;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(canvasGo.transform, false);

            _text = textGo.AddComponent<Text>();
            // Unity renamed the built-in default font at some point ("Arial.ttf" ->
            // "LegacyRuntime.ttf"); try the current name first and fall back rather than
            // guess which one this Editor version ships.
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = _fontSize;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.rectTransform.sizeDelta = new Vector2(300f, 80f);
        }

        /// <summary>Set the number and reset its rise/fade. Call right after <see cref="PoolManager.Spawn"/>.</summary>
        public void Show(float amount)
        {
            _text.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
            var c = _color;
            c.a = 1f;
            _text.color = c;
            _start = transform.position;
        }

        public void OnSpawned() => _age = 0f;
        public void OnDespawned() { }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _lifetime;
            if (t >= 1f)
            {
                _handle.Despawn();
                return;
            }

            transform.position = _start + Vector3.up * (_riseDistance * t);

            var c = _text.color;
            c.a = 1f - t;
            _text.color = c;
        }
    }
}
