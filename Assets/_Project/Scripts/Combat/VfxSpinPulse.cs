using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Cosmetic only: spins its transform and/or breathes its local scale on a sine.
    /// Put it on a projectile's visual child so it never touches the gameplay transform
    /// that colliders and movement read (AI_Guidelines §1). Pool-safe — it only drives
    /// local transform values it fully owns.
    /// </summary>
    [DisallowMultipleComponent]
    public class VfxSpinPulse : MonoBehaviour
    {
        [Tooltip("Degrees per second around Z. 0 = no spin.")]
        [SerializeField] private float _spinDegreesPerSecond = 0f;

        [Tooltip("Peak extra scale at the top of the pulse (0 = no pulse).")]
        [SerializeField, Min(0f)] private float _pulseAmount = 0f;

        [Tooltip("Full pulse cycles per second.")]
        [SerializeField, Min(0f)] private float _pulseFrequency = 2f;

        private Vector3 _baseScale;
        private float _phase;

        private void Awake() => _baseScale = transform.localScale;

        private void OnEnable() => _phase = Random.value * Mathf.PI * 2f;

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_spinDegreesPerSecond != 0f)
                transform.Rotate(0f, 0f, _spinDegreesPerSecond * dt);

            if (_pulseAmount > 0f)
            {
                _phase += _pulseFrequency * Mathf.PI * 2f * dt;
                float k = 1f + Mathf.Sin(_phase) * _pulseAmount * 0.5f + _pulseAmount * 0.5f;
                transform.localScale = _baseScale * k;
            }
        }
    }
}
