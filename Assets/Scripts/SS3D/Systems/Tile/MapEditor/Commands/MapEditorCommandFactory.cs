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
                    new PlaceItemCommand(dto.AssetName, dto.Position, dto.Direction),
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
            {
                GenericObjectSo asset = ctx.ResolveAsset(dto.AssetName);
                if (asset is not TileObjectSo tile || ctx.Construction == null)
                    return false;

                PreviewResult preview = ctx.Construction.TryPreviewTile(
                    tile, dto.Position, dto.Direction, dto.ReplaceExisting);
                if (!preview.CanBuild)
                    return false;

                into.Add(CreatePlaceTile(dto, ctx));
                return true;
            }

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

            into.Add(new PlaceItemCommand(dto.AssetName, dto.Position, dto.Direction));
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
                previousDir);
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
