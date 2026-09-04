namespace SpaceSurvivors.Core
{
    /// <summary>
    /// One finished run, as the leaderboard cares about it.
    ///
    /// <para>A plain value type in Core rather than a reference to <c>RunStats</c> (which lives
    /// in Progression and is a MonoBehaviour): the persistence seams must stay framework-free so
    /// they can be driven from tests and serialised for a backend.</para>
    /// </summary>
    public readonly struct RunResult
    {
        public readonly float SurvivedSeconds;
        public readonly int Kills;
        public readonly int ReachedLevel;
        public readonly int BossesDefeated;

        public RunResult(float survivedSeconds, int kills, int reachedLevel, int bossesDefeated)
        {
            // The server rejects out-of-range values outright, so clamp here rather than send a
            // run it will refuse — a UI glitch should not cost the player their score.
            SurvivedSeconds = survivedSeconds > 0f ? survivedSeconds : 0f;
            Kills = kills > 0 ? kills : 0;
            ReachedLevel = reachedLevel > 1 ? reachedLevel : 1;
            BossesDefeated = bossesDefeated > 0 ? bossesDefeated : 0;
        }
    }
}
