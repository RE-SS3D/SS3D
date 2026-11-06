using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// One object (or floor-decal / spawn-point slot) the map editor Delete tool should clear.
    /// </summary>
    public readonly struct MapEditorDeleteTarget
    {
        public string AssetName { get; init; }
        public TileLayer Layer { get; init; }
        public Direction Direction { get; init; }
        public bool IsFloorDecal { get; init; }
        public bool IsSpawnPoint { get; init; }

        public static MapEditorDeleteTarget Tile(string assetName, TileLayer layer, Direction direction) =>
            new()
            {
                AssetName = assetName,
                Layer = layer,
                Direction = direction,
                IsFloorDecal = false,
                IsSpawnPoint = false,
            };

        public static MapEditorDeleteTarget FloorDecal =>
            new() { IsFloorDecal = true };

        public static MapEditorDeleteTarget SpawnPoint =>
            new() { IsSpawnPoint = true };
    }

    /// <summary>
    /// Maps the object-library subcategory (+ wall-mount face) to clearable placed objects.
    /// </summary>
    public static class MapEditorDeleteTargeting
    {
        private static readonly TileLayer[] PipeLayers =
        {
            TileLayer.PipeLeft,
            TileLayer.PipeRight,
            TileLayer.PipeMiddle,
            TileLayer.PipeSurface,
            TileLayer.PipeManifold,
        };

        /// <summary>
        /// Resolves tile objects to clear for <paramref name="subcategory"/>.
        /// Wall mounts are filtered to <paramref name="direction"/> only.
        /// Overlays are not resolved here — caller checks floor-decal ids on the map.
        /// Returns empty for Uncategorized / item / scripting stubs (caller toasts).
        /// </summary>
        public static void Resolve(
            MapEditorSubcategory subcategory,
            Direction direction,
            ITileLocation[] locations,
            List<MapEditorDeleteTarget> into)
        {
            into.Clear();
            if (locations == null)
                return;

            switch (subcategory)
            {
                case MapEditorSubcategory.Flooring:
                    CollectTurf(locations, TileObjectGenericType.Floor, into);
                    break;
                case MapEditorSubcategory.Walls:
                    CollectTurf(locations, TileObjectGenericType.Wall, into);
                    break;
                case MapEditorSubcategory.Doors:
                    CollectTurf(locations, TileObjectGenericType.Door, into);
                    break;
                case MapEditorSubcategory.Turfs:
                    CollectOtherTurfs(locations, into);
                    if (into.Count == 0)
                        CollectTurf(locations, TileObjectGenericType.Floor, into);
                    break;
                case MapEditorSubcategory.TileObjects:
                    CollectLayer(locations, TileLayer.FurnitureBase, into);
                    CollectLayer(locations, TileLayer.FurnitureTop, into);
                    break;
                case MapEditorSubcategory.WallAttachments:
                    CollectWallMountFace(locations, TileLayer.WallMountLow, direction, into);
                    CollectWallMountFace(locations, TileLayer.WallMountHigh, direction, into);
                    break;
                case MapEditorSubcategory.BaseTiles:
                    CollectLayer(locations, TileLayer.Plenum, into);
                    break;
                case MapEditorSubcategory.Piping:
                    foreach (TileLayer layer in PipeLayers)
                        CollectLayer(locations, layer, into);

                    // The tab holds both, so a wire on a pipe's tile waits until the pipes are gone.
                    if (into.Count == 0)
                        CollectLayer(locations, TileLayer.Wire, into);
                    break;
                case MapEditorSubcategory.Disposals:
                    CollectLayer(locations, TileLayer.Disposal, into);
                    break;
                case MapEditorSubcategory.Overlays:
                case MapEditorSubcategory.Uncategorized:
                case MapEditorSubcategory.FoodDrink:
                case MapEditorSubcategory.Tools:
                case MapEditorSubcategory.Medical:
                case MapEditorSubcategory.Security:
                case MapEditorSubcategory.Misc:
                case MapEditorSubcategory.Atmospherics:
                case MapEditorSubcategory.SpawnPlacements:
                case MapEditorSubcategory.RandomSpawners:
                case MapEditorSubcategory.Triggers:
                    break;
            }
        }

        public static bool IsItemSubcategory(MapEditorSubcategory subcategory) =>
            subcategory is MapEditorSubcategory.FoodDrink
                or MapEditorSubcategory.Tools
                or MapEditorSubcategory.Medical
                or MapEditorSubcategory.Security
                or MapEditorSubcategory.Misc;

        public static bool RequiresSubcategorySelection(MapEditorSubcategory subcategory) =>
            subcategory is MapEditorSubcategory.Uncategorized;

        public static string EmptyHint(MapEditorSubcategory subcategory, Direction direction)
        {
            string label = MapEditorCatalog.GetSubcategoryLabel(subcategory);
            if (subcategory == MapEditorSubcategory.WallAttachments)
                return $"Nothing to delete on {label} ({direction})";

            return $"Nothing to delete on {label}";
        }

        private static void CollectTurf(
            ITileLocation[] locations,
            TileObjectGenericType genericType,
            List<MapEditorDeleteTarget> into)
        {
            ITileLocation location = locations[(int)TileLayer.Turf];
            if (location == null)
                return;

            foreach (PlacedTileObject placed in location.GetAllPlacedObject())
            {
                if (placed != null && placed.GenericType == genericType)
                    into.Add(MapEditorDeleteTarget.Tile(placed.NameString, placed.Layer, placed.Direction));
            }
        }

        private static void CollectOtherTurfs(ITileLocation[] locations, List<MapEditorDeleteTarget> into)
        {
            ITileLocation location = locations[(int)TileLayer.Turf];
            if (location == null)
                return;

            foreach (PlacedTileObject placed in location.GetAllPlacedObject())
            {
                if (placed == null)
                    continue;

                if (placed.GenericType is TileObjectGenericType.Floor
                    or TileObjectGenericType.Wall
                    or TileObjectGenericType.Door)
                    continue;

                into.Add(MapEditorDeleteTarget.Tile(placed.NameString, placed.Layer, placed.Direction));
            }
        }

        private static void CollectLayer(
            ITileLocation[] locations,
            TileLayer layer,
            List<MapEditorDeleteTarget> into)
        {
            int index = (int)layer;
            if (index < 0 || index >= locations.Length)
                return;

            ITileLocation location = locations[index];
            if (location == null)
                return;

            foreach (PlacedTileObject placed in location.GetAllPlacedObject())
            {
                if (placed != null)
                    into.Add(MapEditorDeleteTarget.Tile(placed.NameString, placed.Layer, placed.Direction));
            }
        }

        private static void CollectWallMountFace(
            ITileLocation[] locations,
            TileLayer layer,
            Direction direction,
            List<MapEditorDeleteTarget> into)
        {
            int index = (int)layer;
            if (index < 0 || index >= locations.Length)
                return;

            ITileLocation location = locations[index];
            if (location == null)
                return;

            foreach (PlacedTileObject placed in location.GetAllPlacedObject())
            {
                if (placed != null && placed.Direction == direction)
                    into.Add(MapEditorDeleteTarget.Tile(placed.NameString, placed.Layer, placed.Direction));
            }
        }
    }
}
