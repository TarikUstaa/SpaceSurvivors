using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// One selectable game mode. The menu picks one of these, stashes it in
    /// <see cref="GameSession"/>, and loads the game scene — everything downstream
    /// (spawn curves, win condition) is just this asset (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Game Mode", fileName = "GameMode")]
    public class GameModeData : ScriptableObject
    {
        [Header("Presentation")]
        public string displayName = "Campaign";
        [TextArea] public string description = "";

        [Header("Rules")]
        [Tooltip("Spawn / scaling / boss schedule for this mode.")]
        public DifficultyConfig difficulty;

        [Tooltip("Endless = survive as long as possible, no victory. " +
                 "Off = defeat every scheduled boss to win the mode.")]
        public bool endless = false;
    }
}
