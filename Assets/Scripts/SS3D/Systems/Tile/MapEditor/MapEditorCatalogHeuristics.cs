using SS3D.Systems.Tile.Connections;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Files a tile asset under its palette sub-tab. Tile objects go by the layer they sit on
    /// (+ <see cref="TileObjectGenericType"/> for turfs); items go by their folder under <c>Items/</c>.
    /// The regenerator bakes the result into <c>MapEditorCatalog.asset</c>. A misfiled asset is fixed
    /// by moving it, never by a name rule here.
    /// </summary>
    public static class MapEditorCatalogHeuristics
    {
        /// <summary>Furniture-layer folders that hold furniture; every other furniture-layer object is a machine.</summary>
        private static readonly string[] FurnitureFolders =
        {
            "Furniture/Generic/",
            "Furniture/Flora/",
            "Furniture/Storage/",
        };

        /// <summary><c>Items/</c> folder → sub-tab. First match wins; anything unlisted is Misc.</summary>
        private static readonly (string Folder, MapEditorSubcategory SubTab)[] ItemFolders =
        {
            ("Items/Clothing/", MapEditorSubcategory.Clothing),
            ("Items/Consumables/Chemical/", MapEditorSubcategory.Medical),
            ("Items/Functional/Medical/", MapEditorSubcategory.Medical),
            ("Items/Consumables/Food/", MapEditorSubcategory.FoodAndDrink),
            ("Items/Functional/Tools/", MapEditorSubcategory.Tools),
            ("Items/Functional/Materials/", MapEditorSubcategory.MaterialsAndParts),
            ("Items/Functional/Parts/", MapEditorSubcategory.MaterialsAndParts),
            ("Items/Functional/Devices/", MapEditorSubcategory.Devices),
            ("Items/Weapons/", MapEditorSubcategory.Weapons),
        };

        /// <summary>
        /// Sub-tab → the folders its headings come from (creative-mode §2): the folder just below
        /// a root is the heading, and a file directly in a root, or outside every root, is under <see cref="GeneralHeading"/>.
        /// A sub-tab with no roots has no headings. Rename a folder to change a heading; never add a rule here.
        /// </summary>
        private static readonly (MapEditorSubcategory SubTab, string[] Roots)[] HeadingRoots =
        {
            (MapEditorSubcategory.Furniture, FurnitureFolders),
            (MapEditorSubcategory.Machines, new[] { "Furniture/Machines/" }),
        };

        public const string GeneralHeading = "General";

        private const string ResourcesRoot = "/Resources/";

        public static MapEditorCatalogEntry Infer(GenericObjectSo asset) =>
            Infer(asset, assetPath: null);

        /// <param name="assetPath">
        /// Unity asset path (<c>AssetDatabase.GetAssetPath</c>). Without it (runtime fallback for an
        /// asset missing from the baked catalog) items land in Misc and furniture-layer objects in Machines.
        /// </param>
        /// <returns>Null when the asset has no sub-tab (a turf of an unknown kind, or not a tile asset).</returns>
        public static MapEditorCatalogEntry Infer(GenericObjectSo asset, string assetPath)
        {
            if (asset == null)
                return null;

            string assetName = ResolveAssetName(asset);

            if (asset is ItemObjectSo)
                return Entry(assetName, ClassifyItem(assetPath), assetPath, "item");

            if (asset is not TileObjectSo tile)
                return null;

            MapEditorSubcategory? subTab = ClassifyTile(tile.layer, tile.genericType, assetPath);
            return subTab.HasValue ? Entry(assetName, subTab.Value, assetPath) : null;
        }

        public static MapEditorSubcategory? ClassifyTile(TileLayer layer, TileObjectGenericType genericType, string assetPath = null) =>
            layer switch
            {
                TileLayer.Turf => genericType switch
                {
                    TileObjectGenericType.Floor => MapEditorSubcategory.Floors,
                    TileObjectGenericType.Wall => MapEditorSubcategory.WallsAndWindows,
                    TileObjectGenericType.Door => MapEditorSubcategory.Doors,
                    _ => null,
                },
                TileLayer.WallMountLow or TileLayer.WallMountHigh => MapEditorSubcategory.WallMounts,
                TileLayer.FurnitureBase or TileLayer.FurnitureTop => IsUnderAny(assetPath, FurnitureFolders)
                    ? MapEditorSubcategory.Furniture
                    : MapEditorSubcategory.Machines,
                TileLayer.Plenum => MapEditorSubcategory.Subfloor,
                TileLayer.Wire => MapEditorSubcategory.Cables,
                TileLayer.PipeLeft or TileLayer.PipeRight or TileLayer.PipeMiddle
                    or TileLayer.PipeSurface or TileLayer.PipeManifold => MapEditorSubcategory.GasPipes,
                TileLayer.Disposal => MapEditorSubcategory.DisposalPipes,
                _ => null,
            };

        /// <summary>The item's sub-tab from its folder under <c>Items/</c>; Misc when unlisted or unknown.</summary>
        public static MapEditorSubcategory ClassifyItem(string assetPath)
        {
            string relative = RelativeToResources(assetPath);
            foreach ((string folder, MapEditorSubcategory subTab) in ItemFolders)
            {
                if (relative.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                    return subTab;
            }

            return MapEditorSubcategory.Misc;
        }

        /// <summary>
        /// The grid heading an asset sits under in its sub-tab: "Furniture/Machines/Power/SMES.asset" → "Power".
        /// Empty for a sub-tab without heading roots.
        /// </summary>
        public static string HeadingOf(MapEditorSubcategory subTab, string assetPath)
        {
            string[] roots = null;
            foreach ((MapEditorSubcategory headed, string[] headedRoots) in HeadingRoots)
            {
                if (headed == subTab)
                    roots = headedRoots;
            }

            if (roots == null)
                return string.Empty;

            string relative = RelativeToResources(assetPath);
            foreach (string root in roots)
            {
                if (!relative.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    continue;

                int folderEnd = relative.IndexOf('/', root.Length);
                return folderEnd > root.Length
                    ? MapEditorCatalog.PaletteLabel(relative.Substring(root.Length, folderEnd - root.Length))
                    : GeneralHeading;
            }

            return GeneralHeading;
        }

        /// <summary>Folder-name suffix that makes a folder a variant family.</summary>
        public const string FamilySuffix = "Variants";

        /// <summary>Variant label of a family's base asset, the one named only by the shared prefix.</summary>
        public const string StandardVariant = "Standard";

        /// <summary>
        /// The family folder an asset sits directly in, relative to <c>Resources</c>
        /// ("Furniture/Generic/Seats/Chairs/OfficeChairVariants"), or null when its folder is not a family.
        /// </summary>
        public static string FamilyFolderOf(string assetPath)
        {
            string relative = RelativeToResources(assetPath);
            int slash = relative.LastIndexOf('/');
            if (slash <= 0)
                return null;

            string folder = relative.Substring(0, slash);
            string name = folder.Substring(folder.LastIndexOf('/') + 1);
            return name.Length > FamilySuffix.Length && name.EndsWith(FamilySuffix, StringComparison.Ordinal) ? folder : null;
        }

        /// <summary>"…/OfficeChairVariants" → "Office Chair".</summary>
        public static string FamilyLabelOf(string familyFolder)
        {
            string name = familyFolder.Substring(familyFolder.LastIndexOf('/') + 1);
            return MapEditorCatalog.PaletteLabel(name.Substring(0, name.Length - FamilySuffix.Length));
        }

        /// <summary>
        /// Each member's label: its name minus the prefix all members share (cut back to a word start).
        /// "L"/"R" (with any digits) read Left/Right; an empty remainder is <see cref="StandardVariant"/>.
        /// </summary>
        public static string[] VariantLabels(IReadOnlyList<string> assetNames)
        {
            int prefix = SharedWordPrefix(assetNames);
            string[] labels = new string[assetNames.Count];
            for (int i = 0; i < assetNames.Count; i++)
            {
                string rest = assetNames[i].Substring(prefix);
                Match side = Regex.Match(rest, "^([LR])[0-9]*$");
                labels[i] = rest.Length == 0 ? StandardVariant
                    : side.Success ? (side.Groups[1].Value == "L" ? "Left" : "Right")
                    : MapEditorCatalog.PaletteLabel(rest);
            }

            return labels;
        }

        private static int SharedWordPrefix(IReadOnlyList<string> names)
        {
            if (names.Count == 0)
                return 0;

            int length = names[0].Length;
            foreach (string name in names)
            {
                int common = 0;
                while (common < length && common < name.Length && name[common] == names[0][common])
                    common++;

                length = common;
            }

            // Back up while the cut falls inside a word ("PlantE|arth" → "Plant|Earth").
            while (length > 0 && SplitsAWord(names, length))
                length--;

            return length;
        }

        private static bool SplitsAWord(IReadOnlyList<string> names, int at)
        {
            foreach (string name in names)
            {
                if (at < name.Length && char.IsLower(name[at]))
                    return true;
            }

            return false;
        }

        private static bool IsUnderAny(string assetPath, string[] folders)
        {
            string relative = RelativeToResources(assetPath);
            foreach (string folder in folders)
            {
                if (relative.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>"Assets/…/TileMap/Resources/Items/Weapons/M4.asset" → "Items/Weapons/M4.asset".</summary>
        private static string RelativeToResources(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            string path = assetPath.Replace('\\', '/');
            int root = path.LastIndexOf(ResourcesRoot, StringComparison.OrdinalIgnoreCase);
            return root >= 0 ? path.Substring(root + ResourcesRoot.Length) : path;
        }

        private static string ResolveAssetName(GenericObjectSo asset)
        {
            if (asset.PrefabAsset != null)
                return asset.NameString;

            return string.IsNullOrEmpty(asset.name) ? string.Empty : asset.name;
        }

        private static MapEditorCatalogEntry Entry(string name, MapEditorSubcategory subTab, string assetPath, string extraTag = null)
        {
            MapEditorSubTab row = MapEditorSubTabs.Get(subTab);
            string label = row.Label.ToLowerInvariant();
            return new MapEditorCatalogEntry
            {
                AssetName = name,
                Mode = row.Mode,
                Subcategory = subTab,
                Heading = HeadingOf(subTab, assetPath),
                SearchTags = extraTag == null ? new[] { label } : new[] { extraTag, label },
            };
        }
    }
}
