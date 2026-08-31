using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Every map, in display order. One asset in <c>Resources/MapCatalogue</c> so
    /// <c>MapService</c> and the run-scene <c>EnvironmentDirector</c> share it without
    /// per-scene wiring (M15). The first entry is the default when the profile is blank.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Map Catalogue", fileName = "MapCatalogue")]
    public class MapCatalogue : ScriptableObject
    {
        public List<MapData> maps = new();
    }
}
