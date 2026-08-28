using System.Text;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Data;
using SpaceSurvivors.Progression;
using SpaceSurvivors.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Read-out of the player's current build, shown beside the pause menu. Two aligned
    /// columns (labels / values) rebuilt every time the panel is shown. Pure view — it reads
    /// the <see cref="StatSheet"/>, health, level, run stats and weapon list, changes nothing
    /// (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class StatsPanel : MonoBehaviour
    {
        [SerializeField] private StatSheet _stats;
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private HealthComponent _playerHealth;
        [SerializeField] private ShieldComponent _shield;
        [SerializeField] private LevelSystem _level;
        [SerializeField] private RunStats _runStats;
        [SerializeField] private WeaponController _weapons;

        [SerializeField] private Text _labels;
        [SerializeField] private Text _values;

        private void Awake()
        {
            if (_stats == null) _stats = FindFirstObjectByType<StatSheet>();
            if (_playerHealth == null && _stats != null) _playerHealth = _stats.GetComponent<HealthComponent>();
            if (_shield == null) _shield = FindFirstObjectByType<ShieldComponent>();
            if (_level == null) _level = FindFirstObjectByType<LevelSystem>();
            if (_runStats == null) _runStats = FindFirstObjectByType<RunStats>();
            if (_weapons == null) _weapons = FindFirstObjectByType<WeaponController>();
        }

        private void OnEnable() => Rebuild();

        private void Rebuild()
        {
            if (_labels == null || _values == null) return;

            var l = new StringBuilder();
            var v = new StringBuilder();
            bool first = true;

            void Row(string label, string value) { l.Append(label).Append('\n'); v.Append(value).Append('\n'); }
            void Head(string h)
            {
                if (!first) { l.Append('\n'); v.Append('\n'); }
                first = false;
                l.Append("<b><color=#8fd6ff>").Append(h).Append("</color></b>\n");
                v.Append('\n');
            }

            float Mod(StatId id, float base_) => _stats != null ? _stats.Modify(id, base_) : base_;

            Head("RUN");
            Row("Level", _level != null ? _level.CurrentLevel.ToString() : "1");
            if (_runStats != null)
            {
                int t = Mathf.FloorToInt(_runStats.Seconds);
                Row("Time", $"{t / 60:0}:{t % 60:00}");
                Row("Kills", _runStats.Kills.ToString());
                Row("Scrap", _runStats.Scrap.ToString());
            }

            Head("SHIP");
            float moveBase = _playerConfig != null ? _playerConfig.moveSpeed : 6f;
            Row("Move Speed", Mod(StatId.MoveSpeed, moveBase).ToString("0.0"));
            Row("Max HP", _playerHealth != null ? Mathf.CeilToInt(_playerHealth.Max).ToString() : "-");
            if (_shield != null && _shield.MaxCharges > 0)
                Row("Shield", $"{_shield.CurrentCharges}/{_shield.MaxCharges}");

            Head("OFFENSE");
            Row("Damage", Pct(Mod(StatId.Damage, 1f)));
            Row("Fire Rate", "x" + Mod(StatId.FireRate, 1f).ToString("0.00"));
            Row("Projectiles", "+" + Mathf.RoundToInt(Mod(StatId.ProjectileCount, 0f)));
            Row("Pierce", "+" + Mathf.RoundToInt(Mod(StatId.ProjectilePierce, 0f)));
            Row("Proj. Speed", "x" + Mod(StatId.ProjectileSpeed, 1f).ToString("0.00"));

            Head("UTILITY");
            Row("Pickup Range", "x" + Mod(StatId.PickupRadius, 1f).ToString("0.00"));
            Row("XP Gain", "x" + Mod(StatId.XpGain, 1f).ToString("0.00"));

            if (_weapons != null && _weapons.Weapons.Count > 0)
            {
                Head("WEAPONS");
                foreach (var w in _weapons.Weapons)
                    Row(w != null ? w.displayName : "?", "");
            }

            _labels.text = l.ToString();
            _values.text = v.ToString();
        }

        private static string Pct(float multiplier)
        {
            int p = Mathf.RoundToInt((multiplier - 1f) * 100f);
            return (p >= 0 ? "+" : "") + p + "%";
        }
    }
}
