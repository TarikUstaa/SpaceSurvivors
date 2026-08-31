using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// The full set of <see cref="SpaceEventData"/> the <c>EventDirector</c> can roll from
    /// (M18). One asset in <c>Resources/</c>. Same pattern as <c>MapCatalogue</c> /
    /// <c>UpgradeCatalogue</c> (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Space Event Catalogue", fileName = "SpaceEventCatalogue")]
    public class SpaceEventCatalogue : ScriptableObject
    {
        public List<SpaceEventData> events = new();
    }
}
