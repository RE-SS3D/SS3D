using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Layer groups shown in the tilemap build menu asset dropdown and visibility toggles.
    /// </summary>
    public enum TileLayerCategory
    {
        Subfloor,
        Turfs,
        Furniture,
        WallMounts,
        Cables,
        GasPipes,
        Disposals,
        FloorDecals,
        Items,
        Scripts,
        Areas,
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
                TileLayerCategory.Subfloor => "Subfloor",
                TileLayerCategory.Turfs => "Turfs",
                TileLayerCategory.Furniture => "Furniture",
                TileLayerCategory.WallMounts => "Wall mounts",
                TileLayerCategory.Cables => "Cables",
                TileLayerCategory.GasPipes => "Gas pipes",
                TileLayerCategory.Disposals => "Disposals",
                TileLayerCategory.FloorDecals => "Floor decals",
                TileLayerCategory.Items => "Items",
                TileLayerCategory.Scripts => "Scripts",
                TileLayerCategory.Areas => "Areas",
                _ => category.ToString(),
            };

        public static TileLayerCategory FromDropdownIndex(int index)
        {
            if (index < 0 || index >= AllCategories.Length)
                return TileLayerCategory.Subfloor;

            return AllCategories[index];
        }

        public static int ToDropdownIndex(TileLayerCategory category) => (int)category;

        public static bool IsItemsCategory(TileLayerCategory category) =>
            category == TileLayerCategory.Items;

        public static bool IsFloorDecalsCategory(TileLayerCategory category) =>
            category == TileLayerCategory.FloorDecals;

        public static bool IsScriptsCategory(TileLayerCategory category) =>
            category == TileLayerCategory.Scripts;

        public static IReadOnlyList<TileLayer> GetLayers(TileLayerCategory category) =>
            category switch
            {
                TileLayerCategory.Subfloor => new[] { TileLayer.Plenum },
                TileLayerCategory.Turfs => new[] { TileLayer.Turf },
                TileLayerCategory.Furniture => new[] { TileLayer.FurnitureBase, TileLayer.FurnitureTop },
                TileLayerCategory.WallMounts => new[] { TileLayer.WallMountLow, TileLayer.WallMountHigh },
                TileLayerCategory.Cables => new[] { TileLayer.Wire },
                TileLayerCategory.Disposals => new[] { TileLayer.Disposal },
                TileLayerCategory.GasPipes => new[]
                {
                    TileLayer.PipeLeft,
                    TileLayer.PipeRight,
                    TileLayer.PipeSurface,
                    TileLayer.PipeMiddle,
                    TileLayer.PipeManifold,
                },
                TileLayerCategory.FloorDecals => Array.Empty<TileLayer>(),
                TileLayerCategory.Items => Array.Empty<TileLayer>(),
                TileLayerCategory.Scripts => Array.Empty<TileLayer>(),
                _ => Array.Empty<TileLayer>(),
            };

        public static bool TryGetCategoryForLayer(TileLayer layer, out TileLayerCategory category)
        {
            foreach (TileLayerCategory candidate in AllCategories)
            {
                if (IsItemsCategory(candidate) || IsFloorDecalsCategory(candidate) || IsScriptsCategory(candidate))
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
