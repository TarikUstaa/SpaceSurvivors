using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>Every curse that can be offered in a run. Loaded from Resources by
    /// <see cref="SpaceSurvivors.Progression.UpgradeService"/>, same as the relic catalogue.</summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Progression/Curse Catalogue", fileName = "CurseCatalogue")]
    public class CurseCatalogue : ScriptableObject
    {
        public List<CurseData> curses = new();
    }
}
