using System.Collections.Generic;
using SS3D.Systems.IngameConsoleSystem;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Client: the HUD sprite of a listing's item, as the inventory shows it (<see cref="Item.GetHudSprite"/>,
    /// rendered from the prefab once and cached by asset id). Null when the item can't be found or icons can't
    /// render (headless), so the row keeps its "?" slot.
    /// </summary>
    public static class OperativeUplinkIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite ForItem(string itemAssetId)
        {
            if (string.IsNullOrEmpty(itemAssetId))
            {
                return null;
            }

            if (Cache.TryGetValue(itemAssetId, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = CommandResolver.TryResolveItemAsset(itemAssetId, out Item prefab, out _)
                ? prefab.GetHudSprite()
                : null;
            Cache[itemAssetId] = sprite;
            return sprite;
        }

        /// <summary>Sets <paramref name="slot"/> to the item's icon, or the "?" slot when there is none.</summary>
        public static void Apply(Components.InventorySlot slot, string itemAssetId)
        {
            if (slot == null)
            {
                return;
            }

            Sprite sprite = ForItem(itemAssetId);
            slot.ItemIcon = sprite;
            slot.Unknown = sprite == null;
        }
    }
}
