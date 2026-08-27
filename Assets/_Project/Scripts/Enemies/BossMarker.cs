using SpaceSurvivors.Combat;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Tags an enemy as a boss and does a short scale-in "arrival" pop when it spawns.
    /// The <see cref="SpaceSurvivors.UI.BossHealthBar"/> watches for the live one via
    /// <see cref="Active"/>. Pure marker + cosmetic (AI_Guidelines §1).
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(HealthComponent))]
    [DisallowMultipleComponent]
    public class BossMarker : MonoBehaviour, Core.IPoolable
    {
        public static BossMarker Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active = null;

        [SerializeField] private string _displayName = "MINI-BOSS";
        [SerializeField, Min(0f)] private float _arrivalPopDuration = 0.5f;
        [SerializeField, Min(1f)] private float _arrivalPopFrom = 0.2f;

        public string DisplayName => _displayName;
        public HealthComponent Health { get; private set; }

        private Vector3 _baseScale;
        private float _pop;

        private void Awake()
        {
            Health = GetComponent<HealthComponent>();
            _baseScale = transform.localScale;
        }

        public void OnSpawned()
        {
            Active = this;
            _pop = _arrivalPopDuration;
            transform.localScale = _baseScale * _arrivalPopFrom;
        }

        public void OnDespawned()
        {
            if (Active == this) Active = null;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            if (_pop <= 0f) return;
            _pop -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_pop / Mathf.Max(0.0001f, _arrivalPopDuration));
            transform.localScale = Vector3.Lerp(_baseScale * _arrivalPopFrom, _baseScale, Mathf.SmoothStep(0f, 1f, t));
        }
    }
}
