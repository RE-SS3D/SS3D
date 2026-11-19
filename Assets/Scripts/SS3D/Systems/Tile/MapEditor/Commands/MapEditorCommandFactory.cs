using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    /// <summary>
    /// Builds invertible commands from client DTOs. Previous occupants / decal ids are
    /// snapshotted from the live map so the client does not need to send prior state.
    /// </summary>
    public static class MapEditorCommandFactory
    {
        public static IMapEditorCommand FromDto(MapEditorCommandDto dto, MapEditorCommandContext ctx)
        {
            return dto.Kind switch
            {
                MapEditorCommandKind.PlaceTile => CreatePlaceTile(dto, ctx),
                MapEditorCommandKind.PlaceItem =>
                    new PlaceItemCommand(dto.AssetName, dto.Position, dto.Direction, dto.Setup),
                MapEditorCommandKind.ClearTile =>
                    new ClearTileCommand(dto.AssetName, dto.Position, dto.Direction),
                MapEditorCommandKind.ClearItem =>
                    new ClearItemCommand(dto.AssetName, dto.Position, dto.Direction),
                MapEditorCommandKind.MoveTile =>
                    new MoveTileCommand(dto.AssetName, dto.PreviousPosition, dto.Position, dto.Direction),
                MapEditorCommandKind.SetFloorDecal =>
                    CreateSetFloorDecal(dto.Position, dto.DecalId, ctx),
                MapEditorCommandKind.ClearFloorDecal =>
                    CreateSetFloorDecal(dto.Position, 0, ctx),
                MapEditorCommandKind.PlaceSpawnPoint =>
                    CreatePlaceSpawnPoint(dto, ctx),
                MapEditorCommandKind.ClearSpawnPoint =>
                    CreateClearSpawnPoint(dto.Position, ctx),
                MapEditorCommandKind.PlaceGasMarker =>
                    CreatePlaceGasMarker(dto.GasMarker, ctx),
                MapEditorCommandKind.ClearGasMarker =>
                    CreateClearGasMarker(dto.Position, ctx),
                MapEditorCommandKind.SetObjectSetup =>
                    new SetObjectSetupCommand(dto.AssetName, dto.Position, dto.Direction, dto.PreviousSetup, dto.Setup),
                _ => CreatePlaceTile(dto, ctx),
            };
        }

        public static bool TryCreatePlaceCommands(
            MapEditorCommandDto dto,
            MapEditorCommandContext ctx,
            List<IMapEditorCommand> into)
        {
            if (dto.Kind is not (MapEditorCommandKind.PlaceTile or MapEditorCommandKind.PlaceItem
                or MapEditorCommandKind.SetFloorDecal or MapEditorCommandKind.PlaceSpawnPoint
                or MapEditorCommandKind.PlaceGasMarker))
            {
                into.Add(FromDto(dto, ctx));
                return true;
            }

            if (dto.Kind == MapEditorCommandKind.PlaceTile)
                return TryCreatePlaceTileCommands(dto, ctx, into);

            if (dto.Kind == MapEditorCommandKind.SetFloorDecal)
            {
                if (ctx.Map == null)
                    return false;

                if (!ctx.Map.TryGetTileLocation(TileLayer.Plenum, dto.Position, out ITileLocation plenum)
                    || plenum.IsFullyEmpty())
                    return false;

                into.Add(CreateSetFloorDecal(dto.Position, dto.DecalId, ctx));
                return true;
            }

            if (dto.Kind == MapEditorCommandKind.PlaceSpawnPoint)
            {
                if (ctx.Map == null || ctx.SpawnPoints == null)
                    return false;

                if (!ctx.Map.TryGetTileLocation(TileLayer.Plenum, dto.Position, out ITileLocation plenum)
                    || plenum.IsFullyEmpty())
                    return false;

                if (!MapEditorSpawnCatalog.TryDecode(dto.AssetName, out _, out _, out _))
                    return false;

                into.Add(CreatePlaceSpawnPoint(dto, ctx));
                return true;
            }

            if (dto.Kind == MapEditorCommandKind.PlaceGasMarker)
            {
                if (ctx.Map == null || ctx.GasMarkers == null || !dto.GasMarker.IsValid)
                    return false;

                if (!ctx.Map.TryGetTileLocation(TileLayer.Plenum, dto.GasMarker.Position, out ITileLocation plenum)
                    || plenum.IsFullyEmpty())
                    return false;

                into.Add(CreatePlaceGasMarker(dto.GasMarker, ctx));
                return true;
            }

            into.Add(new PlaceItemCommand(dto.AssetName, dto.Position, dto.Direction, dto.Setup));
            return true;
        }

        /// <summary>
        /// A tile place, checked against the live map by <see cref="MapEditorPlacementRules"/>. Every place in a gesture is
        /// checked before any of them runs, so a subfloor the object needs is added here, ahead of the object, rather than
        /// sent by the client: the object's own check would fail before the subfloor existed. Both land in the gesture's
        /// one compound, which undoes in reverse, so a single undo takes the object and then its subfloor.
        /// </summary>
        private static bool TryCreatePlaceTileCommands(
            MapEditorCommandDto dto,
            MapEditorCommandContext ctx,
            List<IMapEditorCommand> into)
        {
            GenericObjectSo asset = ctx.ResolveAsset(dto.AssetName);
            if (asset is not TileObjectSo tile || ctx.Map == null || ctx.Construction == null)
                return false;

            PlacementPlan plan = MapEditorPlacementRules.Plan(
                ctx.Map, tile, dto.Position, dto.Direction, dto.ReplaceExisting, dto.AutoSubfloor);
            if (!plan.CanPlace)
                return false;

            if (plan.Subfloor.Count > 0)
            {
                if (ctx.ResolveAsset(MapEditorPlacementRules.SubfloorAssetName) is not TileObjectSo subfloor)
                    return false;

                foreach (SubfloorCell cell in plan.Subfloor)
                {
                    into.Add(CreatePlaceTile(new MapEditorCommandDto
                    {
                        Kind = MapEditorCommandKind.PlaceTile,
                        AssetName = subfloor.NameString,
                        Position = cell.Position,
                        Direction = Direction.North,
                        ReplaceExisting = cell.ReplacesGrating,
                    }, ctx));
                }
            }

            dto.ReplaceExisting = plan.Replace;
            into.Add(CreatePlaceTile(dto, ctx));
            return true;
        }

        private static PlaceTileCommand CreatePlaceTile(MapEditorCommandDto dto, MapEditorCommandContext ctx)
        {
            string previousName = null;
            Direction previousDir = Direction.North;

            if (dto.ReplaceExisting && ctx?.Map != null)
            {
                GenericObjectSo asset = ctx.ResolveAsset(dto.AssetName);
                if (asset is TileObjectSo tile
                    && ctx.Map.TryGetTileLocation(tile.layer, dto.Position, out ITileLocation location)
                    && location.TryGetPlacedObject(out PlacedTileObject placed, dto.Direction)
                    && placed != null)
                {
                    previousName = placed.NameString;
                    previousDir = placed.Direction;
                }
            }

            return new PlaceTileCommand(
                dto.AssetName,
                dto.Position,
                dto.Direction,
                dto.ReplaceExisting,
                previousName,
                previousDir,
                dto.Setup);
        }

        private static SetFloorDecalCommand CreateSetFloorDecal(
            Vector3 position, ushort decalId, MapEditorCommandContext ctx)
        {
            ushort previous = 0;
            if (ctx?.Map != null && ctx.Map.TryGetFloorDecalId(position, out ushort existing))
                previous = existing;

            return new SetFloorDecalCommand(position, decalId, previous);
        }

        private static PlaceSpawnPointCommand CreatePlaceSpawnPoint(
            MapEditorCommandDto dto, MapEditorCommandContext ctx)
        {
            SpawnPointRecord record = MapEditorSpawnCatalog.ToRecord(dto.AssetName, dto.Position, dto.Direction);
            SpawnPointRecord previous = default;
            bool hadPrevious = ctx?.SpawnPoints != null &&
                               ctx.SpawnPoints.TryGetAt(dto.Position, out previous);
            return new PlaceSpawnPointCommand(record, hadPrevious, previous);
        }

        private static PlaceGasMarkerCommand CreatePlaceGasMarker(GasMarkerRecord record, MapEditorCommandContext ctx)
        {
            GasMarkerRecord previous = default;
            bool hadPrevious = ctx?.GasMarkers != null && ctx.GasMarkers.TryGetAt(record.Position, out previous);
            return new PlaceGasMarkerCommand(record, hadPrevious, previous);
        }

        private static ClearGasMarkerCommand CreateClearGasMarker(Vector3 position, MapEditorCommandContext ctx)
        {
            GasMarkerRecord previous = default;
            bool hadPrevious = ctx?.GasMarkers != null && ctx.GasMarkers.TryGetAt(position, out previous);
            return new ClearGasMarkerCommand(position, hadPrevious, previous);
        }

        private static ClearSpawnPointCommand CreateClearSpawnPoint(
            Vector3 position, MapEditorCommandContext ctx)
        {
            SpawnPointRecord previous = default;
            bool hadPrevious = ctx?.SpawnPoints != null &&
                               ctx.SpawnPoints.TryGetAt(position, out previous);
            return new ClearSpawnPointCommand(position, hadPrevious, previous);
        }
    }
}
