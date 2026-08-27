using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Anything the player can pick up — scrap/XP, health, magnets, chests. The collector
    /// finds these via <c>GetComponent&lt;ICollectible&gt;()</c> and calls
    /// <see cref="Collect"/>; the collectible decides what that means (AI_Guidelines §2).
    /// </summary>
    public interface ICollectible
    {
        /// <param name="collector">The GameObject that picked this up (usually the player).</param>
        void Collect(GameObject collector);
    }
}
