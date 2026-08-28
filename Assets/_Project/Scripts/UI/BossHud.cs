using SpaceSurvivors.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Mini-boss / boss UI: a flashing "approaching" banner when
    /// <see cref="SpawnDirector.BossIncoming"/> fires, then a name plate + health bar along the
    /// bottom while a <see cref="BossMarker"/> is alive. Prefab-style — wires Inspector
    /// widgets, builds nothing (AI_Guidelines §1, §7). Built by <c>HudBuilder</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossHud : MonoBehaviour
    {
        [SerializeField] private SpawnDirector _spawnDirector;

        [Header("Warning")]
        [SerializeField] private GameObject _warningRoot;
        [SerializeField] private Text _warningText;

        [Header("Health bar")]
        [SerializeField] private GameObject _barRoot;
        [SerializeField] private Text _bossNameText;
        [SerializeField] private Image _hpFill;

        private float _warningTimer;

        private void Awake()
        {
            if (_spawnDirector == null) _spawnDirector = FindFirstObjectByType<SpawnDirector>();
            if (_warningRoot != null) _warningRoot.SetActive(false);
            if (_barRoot != null) _barRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (_spawnDirector != null) _spawnDirector.BossIncoming += HandleBossIncoming;
        }

        private void OnDisable()
        {
            if (_spawnDirector != null) _spawnDirector.BossIncoming -= HandleBossIncoming;
        }

        private void HandleBossIncoming(string bossName, float lead)
        {
            if (_warningText != null)
                _warningText.text = $"!!  {bossName.ToUpperInvariant()}  APPROACHING  !!";
            _warningTimer = Mathf.Max(1.5f, lead + 1f);
            if (_warningRoot != null) _warningRoot.SetActive(true);
        }

        private void Update()
        {
            if (_warningTimer > 0f)
            {
                _warningTimer -= Time.unscaledDeltaTime;
                if (_warningText != null)
                {
                    float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                    _warningText.color = new Color(1f, 0.32f, 0.26f, Mathf.Lerp(0.45f, 1f, a));
                }
                if (_warningTimer <= 0f && _warningRoot != null) _warningRoot.SetActive(false);
            }

            var boss = BossMarker.Active;
            bool show = boss != null && boss.Health != null && boss.Health.IsAlive;
            if (_barRoot != null && _barRoot.activeSelf != show) _barRoot.SetActive(show);
            if (!show) return;

            if (_bossNameText != null) _bossNameText.text = boss.DisplayName;
            if (_hpFill != null) _hpFill.fillAmount = Mathf.Clamp01(boss.Health.Normalized);
        }
    }
}
