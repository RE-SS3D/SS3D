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
    /// Resolves a sub-tab's <see cref="MapEditorDeleteScope"/> to the placed tile objects the Delete tool clears.
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

        public static MapEditorDeleteScope ScopeOf(MapEditorSubcategory subcategory) =>
            MapEditorSubTabs.TryGet(subcategory, out MapEditorSubTab row) ? row.DeleteScope : MapEditorDeleteScope.None;

        /// <summary>
        /// True for scopes that resolve against tile layers here; floor decals, markers, items and rooms
        /// are cleared by the caller from their own registries.
        /// </summary>
        public static bool IsTileScope(MapEditorDeleteScope scope) =>
            scope is MapEditorDeleteScope.FloorTurf
                or MapEditorDeleteScope.WallTurf
                or MapEditorDeleteScope.DoorTurf
                or MapEditorDeleteScope.WallMountFace
                or MapEditorDeleteScope.FurnitureLayers
                or MapEditorDeleteScope.Plenum
                or MapEditorDeleteScope.Wire
                or MapEditorDeleteScope.PipeLayers
                or MapEditorDeleteScope.Disposal;

        public static void Resolve(
            MapEditorSubcategory subcategory,
            Direction direction,
            ITileLocation[] locations,
            List<MapEditorDeleteTarget> into) =>
            Resolve(ScopeOf(subcategory), direction, locations, into);

        /// <summary>
        /// Resolves tile objects to clear for <paramref name="scope"/>. Wall mounts are filtered to
        /// <paramref name="direction"/> only. Non-tile scopes (<see cref="IsTileScope"/>) resolve empty.
        /// </summary>
        public static void Resolve(
            MapEditorDeleteScope scope,
            Direction direction,
            ITileLocation[] locations,
            List<MapEditorDeleteTarget> into)
        {
            into.Clear();
            if (locations == null)
                return;

            switch (scope)
            {
                case MapEditorDeleteScope.FloorTurf:
                    CollectTurf(locations, TileObjectGenericType.Floor, into);
                    break;
                case MapEditorDeleteScope.WallTurf:
                    CollectTurf(locations, TileObjectGenericType.Wall, into);
                    break;
                case MapEditorDeleteScope.DoorTurf:
                    CollectTurf(locations, TileObjectGenericType.Door, into);
                    break;
                case MapEditorDeleteScope.WallMountFace:
                    CollectWallMountFace(locations, TileLayer.WallMountLow, direction, into);
                    CollectWallMountFace(locations, TileLayer.WallMountHigh, direction, into);
                    break;
                case MapEditorDeleteScope.FurnitureLayers:
                    CollectLayer(locations, TileLayer.FurnitureBase, into);
                    CollectLayer(locations, TileLayer.FurnitureTop, into);
                    break;
                case MapEditorDeleteScope.Plenum:
                    CollectLayer(locations, TileLayer.Plenum, into);
                    break;
                case MapEditorDeleteScope.Wire:
                    CollectLayer(locations, TileLayer.Wire, into);
                    break;
                case MapEditorDeleteScope.PipeLayers:
                    foreach (TileLayer layer in PipeLayers)
                        CollectLayer(locations, layer, into);
                    break;
                case MapEditorDeleteScope.Disposal:
                    CollectLayer(locations, TileLayer.Disposal, into);
                    break;
            }
        }

        /// <summary>The "nothing here" toast, naming the sub-tab by its path ("Upper → Wall Mounts").</summary>
        public static string EmptyHint(MapEditorSubcategory subcategory, Direction direction)
        {
            string path = MapEditorSubTabs.PathOf(subcategory);
            if (ScopeOf(subcategory) == MapEditorDeleteScope.WallMountFace)
                return $"Nothing to delete on {path} ({direction})";

            return $"Nothing to delete on {path}";
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
