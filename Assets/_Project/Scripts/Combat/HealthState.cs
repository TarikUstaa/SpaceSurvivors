namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Pure C# health math — no Unity types, no MonoBehaviour. All the clamping and
    /// bookkeeping lives here so it can be covered by fast EditMode tests
    /// (AI_Guidelines §7). <see cref="HealthComponent"/> is a thin wrapper that adds
    /// events, i-frames and the Unity lifecycle on top of this.
    /// </summary>
    public sealed class HealthState
    {
        public float Max { get; private set; }
        public float Current { get; private set; }

        public bool IsAlive => Current > 0f;
        public float Normalized => Max > 0f ? Current / Max : 0f;

        public HealthState(float max)
        {
            Max = max < 0f ? 0f : max;
            Current = Max;
        }

        /// <summary>Add <paramref name="delta"/> (negative = damage). Returns the amount actually applied.</summary>
        public float Apply(float delta)
        {
            float previous = Current;
            Current = Clamp(Current + delta, 0f, Max);
            return Current - previous;
        }

        /// <summary>Change the ceiling. Optionally top the pool back up (e.g. a max-HP upgrade).</summary>
        public void SetMax(float newMax, bool refillToFull)
        {
            Max = newMax < 0f ? 0f : newMax;
            if (refillToFull) Current = Max;
            else Current = Clamp(Current, 0f, Max);
        }

        /// <summary>Restore to full — used when an object is recycled from a pool.</summary>
        public void ResetToFull() => Current = Max;

        private static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);
    }
}
