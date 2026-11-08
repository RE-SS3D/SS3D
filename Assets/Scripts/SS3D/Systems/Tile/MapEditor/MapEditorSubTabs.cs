using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>What the Delete tool clears while a sub-tab is open (one per row of <see cref="MapEditorSubTabs"/>).</summary>
    public enum MapEditorDeleteScope
    {
        None,
        FloorTurf,
        WallTurf,
        DoorTurf,
        /// <summary>Wall mounts on the face the cursor ghost points at.</summary>
        WallMountFace,
        /// <summary><see cref="TileLayer.FurnitureBase"/> and <see cref="TileLayer.FurnitureTop"/>.</summary>
        FurnitureLayers,
        FloorDecal,
        Plenum,
        Wire,
        /// <summary>Every gas pipe layer, <see cref="TileLayer.PipeSurface"/> and <see cref="TileLayer.PipeManifold"/> included.</summary>
        PipeLayers,
        Disposal,
        /// <summary>Items sit off-grid: the item under the cursor, not a tile layer.</summary>
        ItemUnderCursor,
        SpawnPoint,
        GasMarker,
        /// <summary>Room assignments, cleared by the area tool rather than the tile delete path.</summary>
        Area,
    }

    [Flags]
    public enum MapEditorSubTabFlags
    {
        None = 0,
        /// <summary>Holds markers (spawn points, rooms, starting air), not tile objects or items.</summary>
        IsMarker = 1 << 0,
        IsItem = 1 << 1,
        /// <summary>Hidden from sandbox players (<see cref="MapEditorCapabilities.RestrictedPalette"/>).</summary>
        SandboxRestricted = 1 << 2,
    }

    /// <summary>One palette sub-tab: everything the editor knows about it lives on this row.</summary>
    public sealed class MapEditorSubTab
    {
        public MapEditorSubcategory Id { get; init; }
        public MapEditorMode Mode { get; init; }
        /// <summary>Position in the tab, in the order a room gets built (creative-mode §2).</summary>
        public int BuildOrder { get; init; }
        public string Label { get; init; }
        public string IconPath { get; init; }
        public MapEditorDeleteScope DeleteScope { get; init; }
        /// <summary>Entry count at which the grid draws headings.</summary>
        public int HeadingThreshold { get; init; } = MapEditorSubTabs.DefaultHeadingThreshold;
        public MapEditorSubTabFlags Flags { get; init; }

        public bool IsMarker => (Flags & MapEditorSubTabFlags.IsMarker) != 0;
        public bool IsItem => (Flags & MapEditorSubTabFlags.IsItem) != 0;
        public bool SandboxRestricted => (Flags & MapEditorSubTabFlags.SandboxRestricted) != 0;
    }

    /// <summary>
    /// The palette's sub-tab table (<c>creative-mode.md</c> §2). Every label, icon, order, delete scope
    /// and flag is read from here; code couples to a sub-tab by its ID or a flag, never by its label.
    /// </summary>
    public static class MapEditorSubTabs
    {
        public const int DefaultHeadingThreshold = 20;

        private const string Icons = "Assets/Art/Icons/map-editor/";

        private static readonly MapEditorSubTab[] Rows =
        {
            Row(MapEditorSubcategory.Floors, MapEditorMode.Upper, 0, "Floors", "grid", MapEditorDeleteScope.FloorTurf),
            Row(MapEditorSubcategory.WallsAndWindows, MapEditorMode.Upper, 1, "Walls & Windows", "bracket", MapEditorDeleteScope.WallTurf),
            Row(MapEditorSubcategory.Doors, MapEditorMode.Upper, 2, "Doors", "door", MapEditorDeleteScope.DoorTurf),
            Row(MapEditorSubcategory.WallMounts, MapEditorMode.Upper, 3, "Wall Mounts", "pin", MapEditorDeleteScope.WallMountFace),
            Row(MapEditorSubcategory.Furniture, MapEditorMode.Upper, 4, "Furniture", "cube", MapEditorDeleteScope.FurnitureLayers),
            Row(MapEditorSubcategory.Machines, MapEditorMode.Upper, 5, "Machines", "settings", MapEditorDeleteScope.FurnitureLayers),
            Row(MapEditorSubcategory.Decals, MapEditorMode.Upper, 6, "Decals", "layers", MapEditorDeleteScope.FloorDecal),

            Row(MapEditorSubcategory.Subfloor, MapEditorMode.Lower, 0, "Subfloor", "dots9", MapEditorDeleteScope.Plenum),
            Row(MapEditorSubcategory.Cables, MapEditorMode.Lower, 1, "Cables", "bolt", MapEditorDeleteScope.Wire),
            Row(MapEditorSubcategory.GasPipes, MapEditorMode.Lower, 2, "Gas Pipes", "pipe", MapEditorDeleteScope.PipeLayers),
            Row(MapEditorSubcategory.DisposalPipes, MapEditorMode.Lower, 3, "Disposal Pipes", "trash", MapEditorDeleteScope.Disposal),

            Item(MapEditorSubcategory.Tools, 0, "Tools", "settings"),
            Item(MapEditorSubcategory.MaterialsAndParts, 1, "Materials & Parts", "cube"),
            Item(MapEditorSubcategory.Clothing, 2, "Clothing", "box"),
            Item(MapEditorSubcategory.Medical, 3, "Medical", "cube"),
            Item(MapEditorSubcategory.FoodAndDrink, 4, "Food & Drink", "box"),
            Item(MapEditorSubcategory.Devices, 5, "Devices", "bolt"),
            Item(MapEditorSubcategory.Weapons, 6, "Weapons", "bolt"),
            Item(MapEditorSubcategory.Misc, 7, "Misc", "dots9"),

            Row(MapEditorSubcategory.SpawnPoints, MapEditorMode.Scripting, 0, "Spawn Points", "pin", MapEditorDeleteScope.SpawnPoint,
                MapEditorSubTabFlags.IsMarker | MapEditorSubTabFlags.SandboxRestricted),
            Row(MapEditorSubcategory.Rooms, MapEditorMode.Scripting, 1, "Rooms", "grid", MapEditorDeleteScope.Area,
                MapEditorSubTabFlags.IsMarker),
            Row(MapEditorSubcategory.StartingAir, MapEditorMode.Scripting, 2, "Starting Air", "pipe", MapEditorDeleteScope.GasMarker,
                MapEditorSubTabFlags.IsMarker),
        };

        private static readonly Dictionary<MapEditorSubcategory, MapEditorSubTab> ById = Rows.ToDictionary(row => row.Id);

        private static readonly Dictionary<MapEditorMode, MapEditorSubTab[]> ByMode = Rows
            .GroupBy(row => row.Mode)
            .ToDictionary(group => group.Key, group => group.OrderBy(row => row.BuildOrder).ToArray());

        public static IReadOnlyList<MapEditorSubTab> All => Rows;

        public static readonly MapEditorMode[] Modes =
            { MapEditorMode.Upper, MapEditorMode.Lower, MapEditorMode.Items, MapEditorMode.Scripting };

        /// <summary>The sub-tab the editor opens on the first time (creative-mode §2).</summary>
        public const MapEditorSubcategory Default = MapEditorSubcategory.Floors;

        public static MapEditorSubTab Get(MapEditorSubcategory id) =>
            ById.TryGetValue(id, out MapEditorSubTab row)
                ? row
                : throw new ArgumentOutOfRangeException(nameof(id), id, "Not a palette sub-tab.");

        public static bool TryGet(MapEditorSubcategory id, out MapEditorSubTab row) => ById.TryGetValue(id, out row);

        /// <summary>The mode's sub-tabs in build order.</summary>
        public static IReadOnlyList<MapEditorSubTab> InMode(MapEditorMode mode) =>
            ByMode.TryGetValue(mode, out MapEditorSubTab[] rows) ? rows : Array.Empty<MapEditorSubTab>();

        public static string ModeLabel(MapEditorMode mode) =>
            mode switch
            {
                MapEditorMode.Upper => "Upper",
                MapEditorMode.Lower => "Lower",
                MapEditorMode.Items => "Items",
                MapEditorMode.Scripting => "Scripts",
                _ => mode.ToString(),
            };

        public static string LabelOf(MapEditorSubcategory id) =>
            TryGet(id, out MapEditorSubTab row) ? row.Label : id.ToString();

        /// <summary>Where a player finds the sub-tab, as text: "Scripts → Spawn Points".</summary>
        public static string PathOf(MapEditorSubcategory id) =>
            TryGet(id, out MapEditorSubTab row) ? $"{ModeLabel(row.Mode)} → {row.Label}" : id.ToString();

        private static MapEditorSubTab Row(
            MapEditorSubcategory id,
            MapEditorMode mode,
            int order,
            string label,
            string icon,
            MapEditorDeleteScope deleteScope,
            MapEditorSubTabFlags flags = MapEditorSubTabFlags.None) =>
            new()
            {
                Id = id,
                Mode = mode,
                BuildOrder = order,
                Label = label,
                IconPath = Icons + icon + ".svg",
                DeleteScope = deleteScope,
                Flags = flags,
            };

        private static MapEditorSubTab Item(MapEditorSubcategory id, int order, string label, string icon) =>
            Row(id, MapEditorMode.Items, order, label, icon, MapEditorDeleteScope.ItemUnderCursor, MapEditorSubTabFlags.IsItem);
    }

    /// <summary>
    /// The tab the editor reopens on (creative-mode §2), kept per machine in
    /// <see cref="PlayerPrefs"/> as "Mode/SubTab" enum names, so renumbering never misreads it.
    /// </summary>
    public static class MapEditorLastTab
    {
        public const string PrefsKey = "SS3D.MapEditor.LastTab";

        public static string Encode(MapEditorSubcategory id) =>
            MapEditorSubTabs.TryGet(id, out MapEditorSubTab row) ? $"{row.Mode}/{row.Id}" : null;

        /// <summary>
        /// The stored sub-tab, or <see cref="MapEditorSubTabs.Default"/> when the text names no sub-tab, names one
        /// under another mode, or names one <paramref name="isShown"/> rejects (sandbox, emptied).
        /// </summary>
        public static MapEditorSubcategory Decode(string stored, Func<MapEditorSubTab, bool> isShown)
        {
            string[] parts = stored?.Split('/');
            if (parts is { Length: 2 }
                && Enum.TryParse(parts[0], out MapEditorMode mode)
                && Enum.TryParse(parts[1], out MapEditorSubcategory id)
                && !int.TryParse(parts[1], out _)
                && MapEditorSubTabs.TryGet(id, out MapEditorSubTab row)
                && row.Mode == mode
                && (isShown == null || isShown(row)))
                return id;

            return MapEditorSubTabs.Default;
        }

        public static MapEditorSubcategory Load(Func<MapEditorSubTab, bool> isShown) =>
            Decode(PlayerPrefs.GetString(PrefsKey, null), isShown);

        public static void Save(MapEditorSubcategory id)
        {
            string encoded = Encode(id);
            if (encoded == null || PlayerPrefs.GetString(PrefsKey, null) == encoded)
                return;

            PlayerPrefs.SetString(PrefsKey, encoded);
            PlayerPrefs.Save();
        }
    }
}
