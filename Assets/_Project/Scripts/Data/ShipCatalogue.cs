using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Every ship in the hangar, in display order. One asset in
    /// <c>Resources/ShipCatalogue</c> so <c>ShipService</c> and the run-start
    /// <c>ShipApplier</c> share it without per-scene wiring. The first entry is the free
    /// starter ship.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Meta/Ship Catalogue", fileName = "ShipCatalogue")]
    public class ShipCatalogue : ScriptableObject
    {
        public List<ShipData> ships = new();
    }
}
