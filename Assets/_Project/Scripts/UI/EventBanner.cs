using SpaceSurvivors.Data;
using SpaceSurvivors.Environment;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Flashes a short announcement when the <see cref="EventDirector"/> starts a space event
    /// (M18) — "☄ Meteor shower incoming". Prefab-style: the label + optional group are
    /// Inspector references, this only drives text + a fade timer. Reads the director's event,
    /// owns nothing (AI_Guidelines §1, §7).
    /// </summary>
    [DisallowMultipleComponent]
    public class EventBanner : MonoBehaviour
    {
        [SerializeField] private EventDirector _director;
        [SerializeField] private Text _label;
        [SerializeField] private CanvasGroup _group;
        [SerializeField, Min(0.5f)] private float _holdSeconds = 2.4f;
        [SerializeField, Min(0.05f)] private float _fadeSeconds = 0.5f;

        private float _showLeft;

        private void Awake()
        {
            if (_director == null) _director = FindFirstObjectByType<EventDirector>();
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group != null) _group.alpha = 0f;
        }

        private void OnEnable()
        {
            if (_director != null) _director.EventStarted += Show;
        }

        private void OnDisable()
        {
            if (_director != null) _director.EventStarted -= Show;
        }

        private void Show(SpaceEventData data)
        {
            if (data == null) return;
            if (_label != null)
                _label.text = string.IsNullOrEmpty(data.announce) ? data.displayName : data.announce;
            _showLeft = _holdSeconds + _fadeSeconds;
        }

        private void Update()
        {
            if (_group == null || _showLeft <= 0f) return;
            _showLeft -= Time.deltaTime;
            _group.alpha = _showLeft > _fadeSeconds ? 1f : Mathf.Clamp01(_showLeft / _fadeSeconds);
        }
    }
}
