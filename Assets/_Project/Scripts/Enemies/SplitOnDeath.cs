using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// When this enemy dies it blooms into several smaller ones. The children are real
    /// spawns — they count toward the alive total and drop loot / kills like anything else
    /// (via <see cref="SpawnDirector.SpawnEnemyAt"/>). Give the child <see cref="EnemyData"/>
    /// a prefab WITHOUT this component so the split doesn't recurse (AI_Guidelines §1).
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(EnemyBrain))]
    [DisallowMultipleComponent]
    public class SplitOnDeath : MonoBehaviour
    {
        [Tooltip("The smaller enemy this splits into. Its prefab must NOT have SplitOnDeath.")]
        [SerializeField] private EnemyData _childData;
        [SerializeField, Min(1)] private int _childCount = 3;
        [Tooltip("Children appear within this radius of the death point.")]
        [SerializeField, Min(0f)] private float _scatterRadius = 0.7f;

        private HealthComponent _health;
        private SpawnDirector _director;
        private bool _spent;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _director = FindAnyObjectByType<SpawnDirector>();
        }

        private void OnEnable()
        {
            _spent = false;
            _health.Died += HandleDied;
        }

        private void OnDisable() => _health.Died -= HandleDied;

        private void HandleDied(DamageInfo info)
        {
            if (_spent || _childData == null || _director == null) return;
            _spent = true;

            Vector2 origin = transform.position;
            for (int i = 0; i < _childCount; i++)
            {
                Vector2 p = origin + Random.insideUnitCircle * _scatterRadius;
                _director.SpawnEnemyAt(_childData, p);
            }
        }
    }
}
