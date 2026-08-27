using System;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Tracks XP and level for one entity (the player). Pure bookkeeping — it raises events
    /// and never touches the pause, the UI, or the upgrade application (AI_Guidelines §1).
    /// Curve numbers come from <see cref="ProgressionConfig"/> (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] private ProgressionConfig _config;

        public int CurrentLevel { get; private set; } = 1;
        public int XpIntoLevel { get; private set; }
        public int XpForNextLevel { get; private set; }
        public float LevelProgress01 => XpForNextLevel > 0 ? (float)XpIntoLevel / XpForNextLevel : 0f;

        /// <summary>(current xp into level, xp needed) — fired on every XP gain.</summary>
        public event Action<int, int> XpChanged;
        /// <summary>New level number — fired once per level gained (may fire several times in a row).</summary>
        public event Action<int> LeveledUp;

        private void Awake()
        {
            if (_config == null)
                Debug.LogError($"{nameof(LevelSystem)} on '{name}' has no ProgressionConfig assigned.", this);
            XpForNextLevel = _config != null ? _config.CostForLevel(CurrentLevel) : 1;
        }

        private void Start() => XpChanged?.Invoke(XpIntoLevel, XpForNextLevel);

        /// <summary>Award XP. May trigger one or more level-ups.</summary>
        public void AddXp(int amount)
        {
            if (amount <= 0 || _config == null) return;

            XpIntoLevel += amount;

            while (XpIntoLevel >= XpForNextLevel)
            {
                XpIntoLevel -= XpForNextLevel;
                CurrentLevel++;
                XpForNextLevel = _config.CostForLevel(CurrentLevel);
                LeveledUp?.Invoke(CurrentLevel);
            }

            XpChanged?.Invoke(XpIntoLevel, XpForNextLevel);
        }
    }
}
