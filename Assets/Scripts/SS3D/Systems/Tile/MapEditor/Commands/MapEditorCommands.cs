using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    public sealed class PlaceTileCommand : IMapEditorCommand
    {
        private readonly string _assetName;
        private readonly Vector3 _position;
        private readonly Direction _direction;
        private readonly bool _replaceExisting;
        private readonly string _previousAssetName;
        private readonly Direction _previousDirection;

        public PlaceTileCommand(
            string assetName,
            Vector3 position,
            Direction direction,
            bool replaceExisting,
            string previousAssetName = null,
            Direction previousDirection = Direction.North)
        {
            _assetName = assetName;
            _position = position;
            _direction = direction;
            _replaceExisting = replaceExisting;
            _previousAssetName = previousAssetName;
            _previousDirection = previousDirection;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.PlaceTile,
                AssetName = _assetName,
                Position = _position,
                Direction = _direction,
                ReplaceExisting = _replaceExisting,
            };

        public void Apply(MapEditorCommandContext ctx) => Execute(ctx, _position, _direction);

        public void Revert(MapEditorCommandContext ctx)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is not TileObjectSo tile)
                return;

            ctx.Construction.TryClearTile(_position, tile.layer, _direction);

            if (string.IsNullOrEmpty(_previousAssetName))
                return;

            GenericObjectSo previous = ctx.ResolveAsset(_previousAssetName);
            if (previous is TileObjectSo previousTile)
            {
                ctx.Construction.TryPlaceTile(
                    previousTile, _position, _previousDirection, replaceExisting: true, skipBuildCheck: false);
            }
        }

        private void Execute(MapEditorCommandContext ctx, Vector3 position, Direction direction)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is TileObjectSo tile)
                ctx.Construction.TryPlaceTile(tile, position, direction, _replaceExisting, skipBuildCheck: false);
        }
    }

    public sealed class PlaceItemCommand : IMapEditorCommand
    {
        private readonly string _assetName;
        private readonly Vector3 _position;
        private readonly Direction _direction;

        public PlaceItemCommand(string assetName, Vector3 position, Direction direction)
        {
            _assetName = assetName;
            _position = position;
            _direction = direction;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.PlaceItem,
                AssetName = _assetName,
                Position = _position,
                Direction = _direction,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is ItemObjectSo item)
            {
                Quaternion rotation = Quaternion.Euler(0f, TileHelper.GetRotationAngle(_direction), 0f);
                ctx.Construction.TryPlaceItem(item, _position, rotation);
            }
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is ItemObjectSo item)
                ctx.Construction.TryClearItem(_position, item);
        }
    }

    public sealed class ClearTileCommand : IMapEditorCommand
    {
        private readonly string _assetName;
        private readonly Vector3 _position;
        private readonly Direction _direction;

        public ClearTileCommand(string assetName, Vector3 position, Direction direction)
        {
            _assetName = assetName;
            _position = position;
            _direction = direction;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.ClearTile,
                AssetName = _assetName,
                Position = _position,
                Direction = _direction,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is TileObjectSo tile)
                ctx.Construction.TryClearTile(_position, tile.layer, _direction);
        }

        public void Revert(MapEditorCommandContext ctx) =>
            new PlaceTileCommand(_assetName, _position, _direction, replaceExisting: true).Apply(ctx);
    }

    public sealed class ClearItemCommand : IMapEditorCommand
    {
        private readonly string _assetName;
        private readonly Vector3 _position;
        private readonly Direction _direction;

        public ClearItemCommand(string assetName, Vector3 position, Direction direction = Direction.North)
        {
            _assetName = assetName;
            _position = position;
            _direction = direction;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.ClearItem,
                AssetName = _assetName,
                Position = _position,
                Direction = _direction,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is ItemObjectSo item)
                ctx.Construction.TryClearItem(_position, item);
        }

        public void Revert(MapEditorCommandContext ctx) =>
            new PlaceItemCommand(_assetName, _position, _direction).Apply(ctx);
    }

    public sealed class MoveTileCommand : IMapEditorCommand
    {
        private readonly string _assetName;
        private readonly Vector3 _from;
        private readonly Vector3 _to;
        private readonly Direction _direction;

        public MoveTileCommand(string assetName, Vector3 from, Vector3 to, Direction direction)
        {
            _assetName = assetName;
            _from = from;
            _to = to;
            _direction = direction;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.MoveTile,
                AssetName = _assetName,
                Position = _to,
                PreviousPosition = _from,
                Direction = _direction,
            };

        public void Apply(MapEditorCommandContext ctx) => Move(ctx, _from, _to);

        public void Revert(MapEditorCommandContext ctx) => Move(ctx, _to, _from);

        private void Move(MapEditorCommandContext ctx, Vector3 from, Vector3 to)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is not TileObjectSo tile)
                return;

            Direction direction = _direction;
            if (ctx.Map.TryGetTileLocation(tile.layer, from, out ITileLocation location)
                && location.TryGetPlacedObject(out PlacedTileObject placed))
            {
                direction = placed.Direction;
            }

            ctx.Construction.TryClearTile(from, tile.layer, direction);
            ctx.Construction.TryPlaceTile(tile, to, direction, replaceExisting: false, skipBuildCheck: false);
        }
    }

    /// <summary>
    /// Sets a sparse floor-decal id (0 clears). Snapshots previous id for invertibility.
    /// </summary>
    public sealed class SetFloorDecalCommand : IMapEditorCommand
    {
        private readonly Vector3 _position;
        private readonly ushort _decalId;
        private readonly ushort _previousDecalId;

        public SetFloorDecalCommand(Vector3 position, ushort decalId, ushort previousDecalId)
        {
            _position = position;
            _decalId = decalId;
            _previousDecalId = previousDecalId;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = _decalId == 0 ? MapEditorCommandKind.ClearFloorDecal : MapEditorCommandKind.SetFloorDecal,
                Position = _position,
                DecalId = _decalId,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            if (ctx.Map == null)
                return;

            if (ctx.Map.TrySetFloorDecal(_position, _decalId))
                ctx.NotifyFloorDecalsChanged();
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            if (ctx.Map == null)
                return;

            if (ctx.Map.TrySetFloorDecal(_position, _previousDecalId))
                ctx.NotifyFloorDecalsChanged();
        }
    }

    /// <summary>
    /// Places or replaces a spawn marker. Snapshots the previous marker at the tile for undo.
    /// </summary>
    public sealed class PlaceSpawnPointCommand : IMapEditorCommand
    {
        private readonly SpawnPointRecord _record;
        private readonly bool _hadPrevious;
        private readonly SpawnPointRecord _previous;

        public PlaceSpawnPointCommand(SpawnPointRecord record, bool hadPrevious, SpawnPointRecord previous)
        {
            _record = record;
            _hadPrevious = hadPrevious;
            _previous = previous;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.PlaceSpawnPoint,
                AssetName = EncodeAssetName(_record),
                Position = _record.Position,
                Direction = _record.Direction,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            if (ctx.SpawnPoints == null)
                return;

            if (ctx.SpawnPoints.TryPlace(_record))
                ctx.NotifySpawnPointsChanged();
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            if (ctx.SpawnPoints == null)
                return;

            if (_hadPrevious)
            {
                if (ctx.SpawnPoints.TryPlace(_previous))
                    ctx.NotifySpawnPointsChanged();
                return;
            }

            if (ctx.SpawnPoints.TryClearAt(_record.Position, out _))
                ctx.NotifySpawnPointsChanged();
        }

        private static string EncodeAssetName(SpawnPointRecord record) =>
            record.Kind == SpawnPointKind.Job
                ? MapEditorSpawnCatalog.EncodeJob(record.JobName)
                : MapEditorSpawnCatalog.EncodeAntagonist(record.AntagonistCategory);
    }

    /// <summary>
    /// Clears a spawn marker. Snapshots the cleared record for undo.
    /// </summary>
    public sealed class ClearSpawnPointCommand : IMapEditorCommand
    {
        private readonly Vector3 _position;
        private readonly bool _hadPrevious;
        private readonly SpawnPointRecord _previous;

        public ClearSpawnPointCommand(Vector3 position, bool hadPrevious, SpawnPointRecord previous)
        {
            _position = position;
            _hadPrevious = hadPrevious;
            _previous = previous;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.ClearSpawnPoint,
                Position = _position,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            if (ctx.SpawnPoints == null)
                return;

            if (ctx.SpawnPoints.TryClearAt(_position, out _))
                ctx.NotifySpawnPointsChanged();
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            if (ctx.SpawnPoints == null || !_hadPrevious)
                return;

            if (ctx.SpawnPoints.TryPlace(_previous))
                ctx.NotifySpawnPointsChanged();
        }
    }

    /// <summary>
    /// Places or replaces a gas marker. Snapshots the previous marker at the tile for undo.
    /// </summary>
    public sealed class PlaceGasMarkerCommand : IMapEditorCommand
    {
        private readonly GasMarkerRecord _record;
        private readonly bool _hadPrevious;
        private readonly GasMarkerRecord _previous;

        public PlaceGasMarkerCommand(GasMarkerRecord record, bool hadPrevious, GasMarkerRecord previous)
        {
            _record = record;
            _hadPrevious = hadPrevious;
            _previous = previous;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.PlaceGasMarker,
                AssetName = MapEditorGasMarkerCatalog.Encode(_record.Kind),
                Position = _record.Position,
                GasMarker = _record,
            };

        public void Apply(MapEditorCommandContext ctx) => ctx.GasMarkers?.TryPlace(_record);

        public void Revert(MapEditorCommandContext ctx)
        {
            if (_hadPrevious)
                ctx.GasMarkers?.TryPlace(_previous);
            else
                ctx.GasMarkers?.TryClearAt(_record.Position, out _);
        }
    }

    /// <summary>
    /// Clears a gas marker. Snapshots the cleared record for undo.
    /// </summary>
    public sealed class ClearGasMarkerCommand : IMapEditorCommand
    {
        private readonly Vector3 _position;
        private readonly bool _hadPrevious;
        private readonly GasMarkerRecord _previous;

        public ClearGasMarkerCommand(Vector3 position, bool hadPrevious, GasMarkerRecord previous)
        {
            _position = position;
            _hadPrevious = hadPrevious;
            _previous = previous;
        }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.ClearGasMarker,
                Position = _position,
            };

        public void Apply(MapEditorCommandContext ctx) => ctx.GasMarkers?.TryClearAt(_position, out _);

        public void Revert(MapEditorCommandContext ctx)
        {
            if (_hadPrevious)
                ctx.GasMarkers?.TryPlace(_previous);
        }
    }

    public sealed class CompoundCommand : IMapEditorCommand
    {
        private readonly IMapEditorCommand[] _children;

        public CompoundCommand(IMapEditorCommand[] children) => _children = children;

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.Compound,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            foreach (IMapEditorCommand child in _children)
                child.Apply(ctx);
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            for (int i = _children.Length - 1; i >= 0; i--)
                _children[i].Revert(ctx);
        }
    }
}
