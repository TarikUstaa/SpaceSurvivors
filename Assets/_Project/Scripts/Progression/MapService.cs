using System;
using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// The map layer (M15): which arena the next run uses. Static, same shape as
    /// <see cref="ShipService"/> — one catalogue asset both the map-select screen and the run
    /// scene need. Maps are all free, so this is just "list / selected / select"; the choice
    /// is persisted on <see cref="PlayerProfile.selectedMapId"/> through
    /// <see cref="ProfileService"/>.
    /// </summary>
    public static class MapService
    {
        private const string CatalogueResource = "MapCatalogue";

        private static MapCatalogue _catalogue;
        private static readonly List<MapData> _empty = new();

        /// <summary>Raised after a selection change.</summary>
        public static event Action Changed;

        public static IReadOnlyList<MapData> Maps
        {
            get { EnsureLoaded(); return _catalogue != null ? _catalogue.maps : _empty; }
        }

        /// <summary>Test / editor hook.</summary>
        public static void SetCatalogue(MapCatalogue catalogue) => _catalogue = catalogue;

        private static void EnsureLoaded()
        {
            if (_catalogue == null)
                _catalogue = Resources.Load<MapCatalogue>(CatalogueResource);
            if (_catalogue == null)
                Debug.LogWarning($"[Map] no '{CatalogueResource}' in a Resources folder — maps unavailable.");
        }

        private static string DefaultId
        {
            get
            {
                EnsureLoaded();
                return _catalogue != null && _catalogue.maps.Count > 0 && _catalogue.maps[0] != null
                    ? _catalogue.maps[0].id
                    : "open_space";
            }
        }

        public static MapData Find(string id)
        {
            EnsureLoaded();
            if (_catalogue == null) return null;
            foreach (var m in _catalogue.maps)
                if (m != null && m.id == id) return m;
            return null;
        }

        /// <summary>Selected map id, falling back to the first catalogue entry when the profile
        /// is blank or points at a map that no longer exists.</summary>
        public static string SelectedId
        {
            get
            {
                string id = ProfileService.Current.selectedMapId;
                if (!string.IsNullOrEmpty(id) && Find(id) != null) return id;
                return DefaultId;
            }
        }

        public static MapData Selected => Find(SelectedId);

        /// <summary>Equip a map for the next run. No-op if it isn't in the catalogue.</summary>
        public static bool Select(MapData map)
        {
            if (map == null || Find(map.id) == null) return false;
            if (ProfileService.Current.selectedMapId == map.id) return true;

            ProfileService.Current.selectedMapId = map.id;
            ProfileService.Save();
            Changed?.Invoke();
            return true;
        }
    }
}
