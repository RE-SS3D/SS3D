using System.Collections.Generic;
using SS3D.Systems.Gamemodes.Uplink;
using SS3D.Systems.IngameConsoleSystem;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Client: the HUD sprite of a listing's first item, as the inventory shows it (<see cref="Item.GetHudSprite"/>,
    /// rendered from the prefab once and cached). Null when the item can't be found or icons can't render
    /// (headless), so the card keeps its "?" slot.
    /// </summary>
    public static class OperativeUplinkIcons
    {
        private static readonly Dictionary<int, Sprite> Cache = new();

        public static Sprite ForListing(int listingIndex)
        {
            if (Cache.TryGetValue(listingIndex, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = null;
            UplinkListing listing = OperativeUplinkCatalog.At(listingIndex);
            if (listing != null && CommandResolver.TryResolveItemAsset(listing.ItemNames[0], out Item prefab, out _))
            {
                sprite = prefab.GetHudSprite();
            }

            Cache[listingIndex] = sprite;
            return sprite;
        }

        /// <summary>Sets <paramref name="slot"/> to the listing's icon, or the "?" slot when there is none.</summary>
        public static void Apply(Components.InventorySlot slot, int listingIndex)
        {
            if (slot == null)
            {
                return;
            }

            Sprite sprite = listingIndex >= 0 ? ForListing(listingIndex) : null;
            slot.ItemIcon = sprite;
            slot.Unknown = sprite == null;
        }
    }
}
