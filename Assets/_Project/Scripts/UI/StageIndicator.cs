using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using SpaceSurvivors.Game;
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
            bool show = !GameSession.IsEndless && _spawnDirector != null && _spawnDirector.BossStageCount > 0;
            if (_root != null) _root.SetActive(show);
            enabled = show;

            var run = FindFirstObjectByType<RunController>();
            if (run != null) run.RunEnded += (_, __) => { if (_root != null) _root.SetActive(false); };
        }

        private void Update()
        {
            if (_spawnDirector == null || _label == null) return;

            // Both halves must count schedule ENTRIES. BossesDefeated counts boss enemies, and an
            // entry can spawn two, so using it here made the stage run ahead of the fight the
            // player was actually in — the Clamp hid it as a premature "STAGE 5/5".
            int total = _spawnDirector.BossStageCount + 1;
            int stage = Mathf.Clamp(_spawnDirector.BossStagesCleared + 1, 1, total);
            if (stage == _lastStage) return;

            _lastStage = stage;
            _label.text = $"STAGE {stage}/{total}";
        }
    }
}
