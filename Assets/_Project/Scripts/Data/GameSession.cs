using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// The one piece of state that survives a scene load: which <see cref="GameModeData"/>
    /// the player picked in the menu. A plain static holder (no logic) — the single allowed
    /// exception to "no statics", and it lives in Data so every layer can read it
    /// (AI_Guidelines §7). Reset on play so the editor always starts clean.
    /// </summary>
    public static class GameSession
    {
        public static GameModeData SelectedMode { get; set; }

        /// <summary>True when there is no win condition (no mode picked, or an endless mode).</summary>
        public static bool IsEndless => SelectedMode == null || SelectedMode.endless;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => SelectedMode = null;
    }
}
