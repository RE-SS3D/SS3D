using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Layer groups shown in the tilemap build menu asset dropdown and visibility toggles.
    /// </summary>
    public enum TileLayerCategory
    {
        Plenums,
        Turfs,
        Furniture,
        WallMounts,
        WiresAndPipes,
        Overlays,
        Items,
    }

    /// <summary>
    /// Maps build-menu categories to tile layers and dropdown indices.
    /// </summary>
    public static class TileLayerCategoryMapping
    {
        public static readonly TileLayerCategory[] AllCategories =
            (TileLayerCategory[])Enum.GetValues(typeof(TileLayerCategory));

        public static string GetDisplayName(TileLayerCategory category) =>
            category switch
            {
                TileLayerCategory.Plenums => "Plenums",
                TileLayerCategory.Turfs => "Turfs",
                TileLayerCategory.Furniture => "Furniture",
                TileLayerCategory.WallMounts => "Wall mounts",
                TileLayerCategory.WiresAndPipes => "Wires and pipes",
                TileLayerCategory.Overlays => "Overlays",
                TileLayerCategory.Items => "Items",
                _ => category.ToString(),
            };

        public static TileLayerCategory FromDropdownIndex(int index)
        {
            if (index < 0 || index >= AllCategories.Length)
                return TileLayerCategory.Plenums;

            return AllCategories[index];
        }

        public static int ToDropdownIndex(TileLayerCategory category) => (int)category;

        public static bool IsItemsCategory(TileLayerCategory category) =>
            category == TileLayerCategory.Items;

        public static IReadOnlyList<TileLayer> GetLayers(TileLayerCategory category) =>
            category switch
            {
                TileLayerCategory.Plenums => new[] { TileLayer.Plenum },
                TileLayerCategory.Turfs => new[] { TileLayer.Turf },
                TileLayerCategory.Furniture => new[] { TileLayer.FurnitureBase, TileLayer.FurnitureTop },
                TileLayerCategory.WallMounts => new[] { TileLayer.WallMountLow, TileLayer.WallMountHigh },
                TileLayerCategory.WiresAndPipes => new[]
                {
                    TileLayer.Wire,
                    TileLayer.Disposal,
                    TileLayer.PipeLeft,
                    TileLayer.PipeRight,
                    TileLayer.PipeSurface,
                    TileLayer.PipeMiddle,
                },
                TileLayerCategory.Overlays => new[] { TileLayer.Overlays },
                TileLayerCategory.Items => Array.Empty<TileLayer>(),
                _ => Array.Empty<TileLayer>(),
            };

        public static bool TryGetCategoryForLayer(TileLayer layer, out TileLayerCategory category)
        {
            foreach (TileLayerCategory candidate in AllCategories)
            {
                if (IsItemsCategory(candidate))
                    continue;

                foreach (TileLayer mappedLayer in GetLayers(candidate))
                {
                    if (mappedLayer == layer)
                    {
                        category = candidate;
                        return true;
                    }
                }
            }

            category = default;
            return false;
        }
    }
}
