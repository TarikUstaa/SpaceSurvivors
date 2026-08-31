using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// The full list of permanent upgrades on sale. One asset, loaded from
    /// <c>Resources/MetaUpgradeCatalogue</c> by <c>MetaProgressionService</c> so both the shop
    /// scene and the game scene see the same catalogue without wiring it twice.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Meta/Meta Upgrade Catalogue", fileName = "MetaUpgradeCatalogue")]
    public class MetaUpgradeCatalogue : ScriptableObject
    {
        public List<MetaUpgradeData> upgrades = new();
    }
}
