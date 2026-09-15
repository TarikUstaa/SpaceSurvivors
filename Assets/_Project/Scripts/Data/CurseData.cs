using System.Collections.Generic;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// A bargain offered on the level-up screen: a bonus large enough to want, paid for with
    /// something that hurts. Both halves are ordinary <see cref="StatModifier"/>s through the
    /// same <see cref="StatSheet"/> pipeline every upgrade uses — the only thing that makes a
    /// curse a curse is that it also takes something away.
    ///
    /// <para>The two lists are kept apart rather than merged into one (a cost is just a
    /// negative modifier, after all) so the card can show them as what they are: what you get,
    /// and what it costs. A player deciding whether to take one needs to read both at a glance.
    /// </para>
    ///
    /// <para>Costs must be stats that actually move. <c>DamageResist</c> is clamped to 0–0.85
    /// where it is read, so a negative one would silently do nothing to a player who has no
    /// armour yet — a cost that is free for some players and not others is not a bargain, it
    /// is a bug.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Progression/Curse", fileName = "Curse")]
    public class CurseData : ScriptableObject
    {
        [Header("Identity")]
        public string id = "curse";
        public string displayName = "Curse";
        public Sprite icon;

        [Header("Card text — written, not generated, so it can read like the game talks")]
        [Tooltip("The upside, e.g. \"+50% damage\".")]
        public string boonText = "";
        [Tooltip("The price, e.g. \"-30% max health\".")]
        public string costText = "";

        [Header("Granted once, kept for the rest of the run")]
        public List<StatModifier> boons = new();
        public List<StatModifier> costs = new();

        [Header("Draft rules")]
        [Tooltip("Relative chance of being the curse offered, when one is offered at all.")]
        [Min(0f)] public float weight = 1f;
    }
}
