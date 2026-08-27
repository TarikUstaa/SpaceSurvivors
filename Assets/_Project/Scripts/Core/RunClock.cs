using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Single source of truth for "how long the player has survived this run". Every
    /// time-based system — difficulty scaling, the 3:00 mini-boss, run-stats — reads
    /// <see cref="Elapsed"/> from here instead of keeping its own timer (AI_Guidelines §1).
    ///
    /// Uses scaled <see cref="Time.deltaTime"/>, so it freezes automatically during the
    /// level-up pause (Time.timeScale = 0) in M5.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunClock : MonoBehaviour
    {
        /// <summary>Seconds survived so far.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Set false to pause the clock without touching Time.timeScale.</summary>
        public bool Running { get; set; } = true;

        public float ElapsedMinutes => Elapsed / 60f;

        private void Update()
        {
            if (Running) Elapsed += Time.deltaTime;
        }

        public void ResetClock() => Elapsed = 0f;
    }
}
