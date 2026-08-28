using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Campaign-only "STAGE n/N" readout. A scripted run has N scheduled bosses, so N+1 stages
    /// (before the first boss, between bosses, the final-boss stage). Hidden entirely in
    /// endless modes. Reads <see cref="SpawnDirector"/>; owns nothing (§1).
    /// </summary>
    [DisallowMultipleComponent]
    public class StageIndicator : MonoBehaviour
    {
        [SerializeField] private SpawnDirector _spawnDirector;
        [SerializeField] private Text _label;
        [SerializeField] private GameObject _root;

        private int _lastStage = -1;

        private void Awake()
        {
            if (_spawnDirector == null) _spawnDirector = FindFirstObjectByType<SpawnDirector>();
            bool show = !GameSession.IsEndless && _spawnDirector != null && _spawnDirector.ScheduledBossCount > 0;
            if (_root != null) _root.SetActive(show);
            enabled = show;
        }

        private void Update()
        {
            if (_spawnDirector == null || _label == null) return;

            int total = _spawnDirector.ScheduledBossCount + 1;
            int stage = Mathf.Clamp(_spawnDirector.BossesDefeated + 1, 1, total);
            if (stage == _lastStage) return;

            _lastStage = stage;
            _label.text = $"STAGE {stage}/{total}";
        }
    }
}
