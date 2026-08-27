namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Contract for anything that can receive damage — the player, every enemy, destructible
    /// props, the mini-boss. Attackers (projectiles, contact damage) resolve targets via
    /// <c>GetComponent&lt;IDamageable&gt;()</c> and never reference a concrete type
    /// (AI_Guidelines §2).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>False once health has reached zero. Attackers should skip dead targets.</summary>
        bool IsAlive { get; }

        /// <summary>
        /// Apply a damage instance. Implementations decide about i-frames, armour, etc.
        /// Returns <c>true</c> if the hit actually landed (target was alive and vulnerable),
        /// <c>false</c> if it was ignored — attackers use this to avoid being consumed by a
        /// target that is already dead or invulnerable.
        /// </summary>
        bool TakeDamage(in DamageInfo info);
    }
}
