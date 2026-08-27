namespace SpaceSurvivors.Core
{
    /// <summary>
    /// A component that gets first look at incoming damage on the same entity and may fully
    /// absorb it (shields, parries, phase-dodge). <see cref="IDamageable"/> implementations
    /// consult every interceptor before applying HP loss (AI_Guidelines §1, §2).
    /// </summary>
    public interface IDamageInterceptor
    {
        /// <returns>True if this interceptor consumed the hit — no HP should be lost.</returns>
        bool Intercept(in DamageInfo info);
    }
}
