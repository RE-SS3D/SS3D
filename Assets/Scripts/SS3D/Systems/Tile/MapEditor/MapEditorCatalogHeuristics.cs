using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.Connections;
using System;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Heuristic mapping from tile assets to library taxonomy when not explicitly cataloged.
    /// </summary>
    public static class MapEditorCatalogHeuristics
    {
        public static MapEditorCatalogEntry Infer(GenericObjectSo asset) =>
            Infer(asset, assetPath: null);

        /// <param name="assetPath">
        /// Optional Unity asset path (e.g. from <c>AssetDatabase.GetAssetPath</c>). Folder segments
        /// under <c>Items/</c> are preferred over bare name keywords when classifying items.
        /// </param>
        public static MapEditorCatalogEntry Infer(GenericObjectSo asset, string assetPath)
        {
            string assetName = ResolveAssetName(asset);

            if (asset is ItemObjectSo)
                return InferItem(assetName, assetPath);

            if (asset is not TileObjectSo tile)
                return Uncategorized(assetName);

            return tile.layer switch
            {
                TileLayer.Turf when tile.genericType == TileObjectGenericType.Wall =>
                    Entry(assetName, MapEditorMode.Upper, MapEditorSubcategory.Walls, "wall"),
                TileLayer.Turf when tile.genericType == TileObjectGenericType.Door =>
                    Entry(assetName, MapEditorMode.Upper, MapEditorSubcategory.Doors, "door"),
                TileLayer.Turf when tile.genericType == TileObjectGenericType.Floor =>
                    Entry(assetName, MapEditorMode.Upper, MapEditorSubcategory.Flooring, "floor", "plating"),
                TileLayer.Turf =>
                    Entry(assetName, MapEditorMode.Upper, MapEditorSubcategory.Turfs, "turf"),
                TileLayer.Plenum when tile.genericType == TileObjectGenericType.Plenum =>
                    Entry(assetName, MapEditorMode.Lower, MapEditorSubcategory.BaseTiles, "plenum"),
                TileLayer.Plenum =>
                    Entry(assetName, MapEditorMode.Lower, MapEditorSubcategory.BaseTiles, "lattice", "catwalk", "base"),
                TileLayer.FurnitureBase or TileLayer.FurnitureTop =>
                    Entry(assetName, MapEditorMode.Upper, MapEditorSubcategory.TileObjects, "furniture"),
                TileLayer.WallMountLow or TileLayer.WallMountHigh =>
                    Entry(assetName, MapEditorMode.Upper, MapEditorSubcategory.WallAttachments, "wall mount"),
                TileLayer.Disposal =>
                    Entry(assetName, MapEditorMode.Lower, MapEditorSubcategory.Disposals, "disposal"),
                TileLayer.Wire =>
                    Entry(assetName, MapEditorMode.Lower, MapEditorSubcategory.Piping, "wire", "cable"),
                TileLayer.PipeLeft or TileLayer.PipeRight or TileLayer.PipeSurface or TileLayer.PipeMiddle
                    when tile.genericType == TileObjectGenericType.Pipe =>
                    Entry(assetName, MapEditorMode.Lower, MapEditorSubcategory.Piping, "pipe"),
                TileLayer.PipeSurface or TileLayer.PipeManifold =>
                    Entry(assetName, MapEditorMode.Lower, MapEditorSubcategory.Piping, "pipe", "device"),
                _ => Uncategorized(assetName),
            };
        }

        private static string ResolveAssetName(GenericObjectSo asset)
        {
            if (asset == null)
                return string.Empty;

            if (asset.PrefabAsset != null)
                return asset.NameString;

            return string.IsNullOrEmpty(asset.name) ? string.Empty : asset.name;
        }

        public static MapEditorCatalogEntry CreateEraserEntry() =>
            new()
            {
                AssetName = "__eraser__",
                Mode = MapEditorMode.Upper,
                Subcategory = MapEditorSubcategory.Walls,
                SearchTags = new[] { "eraser", "delete", "clear" },
                IsEraser = true,
            };

        /// <summary>
        /// Classifies an item by Resources folder path when available, else by name keywords.
        /// Mirrors <c>Assets/Content/Data/TileMap/Resources/Items/</c> taxonomy.
        /// </summary>
        public static MapEditorCatalogEntry InferItem(string assetName, string assetPath = null)
        {
            MapEditorSubcategory subcategory = ClassifyItem(assetName, assetPath);
            return Entry(
                assetName,
                MapEditorMode.Items,
                subcategory,
                BuildItemTags(assetName, subcategory));
        }

        public static MapEditorSubcategory ClassifyItem(string assetName, string assetPath = null)
        {
            string haystack = BuildItemHaystack(assetName, assetPath);

            // Path / name order: medical and food before broad "tool" matches.
            if (MatchesAny(haystack,
                    "/consumables/chemical/", "/tools/medical/",
                    "medkit", "medicalpatch", "brutepatch", "burnpatch",
                    "healthscanner", "syringe", "bandage", "pill"))
                return MapEditorSubcategory.Medical;

            if (MatchesAny(haystack,
                    "/consumables/food/", "/food/", "/drinks/",
                    "sodacan", "donkpocket", "mug", "food", "drink", "snack"))
                return MapEditorSubcategory.FoodDrink;

            if (MatchesAny(haystack,
                    "/weapons/",
                    "security", "weapon", "gun", "rifle", "pistol", "taser", "baton", "handcuff",
                    "m4", "jackboot"))
                return MapEditorSubcategory.Security;

            if (MatchesAny(haystack,
                    "/functional/tools/", "/functional/materials/", "/functional/parts/",
                    "crowbar", "wrench", "screwdriver", "wirecutter", "welder", "multitool",
                    "hatchet", "knife", "toolbox", "toolbelt", "flashlight",
                    "cable", "wire", "sheet", "rod", "plank", "powercell", "matterbin",
                    "microlaser", "steel", "glass", "wood"))
                return MapEditorSubcategory.Tools;

            return MapEditorSubcategory.Misc;
        }

        private static string BuildItemHaystack(string assetName, string assetPath)
        {
            string name = assetName ?? string.Empty;
            string path = assetPath ?? string.Empty;
            // Normalize so "/Items/Functional/Tools/Medical/X" and "Crowbar" both match.
            return (path + "/" + name).Replace('\\', '/').ToLowerInvariant();
        }

        private static bool MatchesAny(string haystack, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (haystack.Contains(needle, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string[] BuildItemTags(string assetName, MapEditorSubcategory subcategory)
        {
            string categoryTag = subcategory switch
            {
                MapEditorSubcategory.FoodDrink => "food",
                MapEditorSubcategory.Tools => "tool",
                MapEditorSubcategory.Medical => "medical",
                MapEditorSubcategory.Security => "security",
                MapEditorSubcategory.Misc => "misc",
                _ => "item",
            };

            return new[] { "item", categoryTag, assetName };
        }

        private static MapEditorCatalogEntry Entry(string name, MapEditorMode mode, MapEditorSubcategory sub, params string[] tags) =>
            new()
            {
                AssetName = name,
                Mode = mode,
                Subcategory = sub,
                SearchTags = tags,
            };

        private static MapEditorCatalogEntry Uncategorized(string name) =>
            Entry(name, MapEditorMode.Upper, MapEditorSubcategory.Uncategorized, "uncategorized");
    }
}
