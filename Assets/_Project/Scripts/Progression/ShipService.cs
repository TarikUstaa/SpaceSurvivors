using System;
using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// The hangar layer (M14b): which ships are owned, which is selected, buy / select.
    /// Static like <see cref="MetaProgressionService"/> — the catalogue is one config asset
    /// both the hangar scene and the game scene need. Purchases funnel through
    /// <see cref="ProfileService"/> so the wallet stays the single seam (Project_Goals §8).
    ///
    /// The first catalogue entry is the free starter ship: always owned, and the default
    /// selection when the profile hasn't picked one.
    /// </summary>
    public static class ShipService
    {
        private const string CatalogueResource = "ShipCatalogue";

        private static ShipCatalogue _catalogue;
        private static readonly List<ShipData> _empty = new();

        /// <summary>Raised after a buy or a selection change.</summary>
        public static event Action Changed;

        public static IReadOnlyList<ShipData> Ships
        {
            get { EnsureLoaded(); return _catalogue != null ? _catalogue.ships : _empty; }
        }

        /// <summary>Test / editor hook.</summary>
        public static void SetCatalogue(ShipCatalogue catalogue) => _catalogue = catalogue;

        private static void EnsureLoaded()
        {
            if (_catalogue == null)
                _catalogue = Resources.Load<ShipCatalogue>(CatalogueResource);
            if (_catalogue == null)
                Debug.LogWarning($"[Ship] no '{CatalogueResource}' in a Resources folder — hangar will be empty.");
        }

        private static string StarterId
        {
            get
            {
                EnsureLoaded();
                return _catalogue != null && _catalogue.ships.Count > 0 && _catalogue.ships[0] != null
                    ? _catalogue.ships[0].id
                    : "starter";
            }
        }

        public static ShipData Find(string id)
        {
            EnsureLoaded();
            if (_catalogue == null) return null;
            foreach (var s in _catalogue.ships)
                if (s != null && s.id == id) return s;
            return null;
        }

        public static bool IsOwned(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id == StarterId) return true;
            var ship = Find(id);
            if (ship != null && ship.IsFree) return true;
            return ProfileService.Current.ownedShipIds.Contains(id);
        }

        /// <summary>The selected ship id, defaulting to the starter when the profile is blank
        /// or points at a ship that no longer exists / isn't owned.</summary>
        public static string SelectedId
        {
            get
            {
                string id = ProfileService.Current.selectedShipId;
                if (!string.IsNullOrEmpty(id) && Find(id) != null && IsOwned(id)) return id;
                return StarterId;
            }
        }

        public static ShipData Selected => Find(SelectedId);
        public static Sprite SelectedSprite => Selected != null ? Selected.sprite : null;

        public static long CostOf(ShipData ship) => ship != null ? ship.cost : 0;

        public static bool CanAfford(ShipData ship)
            => ship != null && !IsOwned(ship.id) && ProfileService.Wallet >= ship.cost;

        /// <summary>Unlock a ship. Deducts scrap, records ownership, saves, and auto-selects it —
        /// spend + grant as one transaction through <see cref="ProfileService.TryPurchase"/>.</summary>
        public static bool TryBuy(ShipData ship)
        {
            if (ship == null || IsOwned(ship.id)) return false;

            bool bought = ProfileService.TryPurchase(ship.cost, () =>
            {
                var owned = ProfileService.Current.ownedShipIds;
                if (!owned.Contains(ship.id)) owned.Add(ship.id);
                ProfileService.Current.selectedShipId = ship.id;
            });
            if (!bought) return false;

            Changed?.Invoke();
            return true;
        }

        /// <summary>Equip an owned ship. No-op if it isn't owned.</summary>
        public static bool Select(ShipData ship)
        {
            if (ship == null || !IsOwned(ship.id)) return false;
            if (ProfileService.Current.selectedShipId == ship.id) return true;

            ProfileService.Current.selectedShipId = ship.id;
            ProfileService.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Run-start modifiers for the selected ship.</summary>
        public static List<StatModifier> BuildStartingModifiers()
        {
            var list = new List<StatModifier>();
            var ship = Selected;
            if (ship != null && ship.runStartModifiers != null)
                list.AddRange(ship.runStartModifiers);
            return list;
        }
    }
}
