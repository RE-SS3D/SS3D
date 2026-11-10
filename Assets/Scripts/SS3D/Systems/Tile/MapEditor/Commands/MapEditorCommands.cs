using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    public sealed class PlaceTileCommand : IMapEditorCommand, IReportsChange
    {
        private readonly string _assetName;
        private readonly Vector3 _position;
        private readonly Direction _direction;
        private readonly bool _replaceExisting;
        private readonly string _previousAssetName;
        private readonly Direction _previousDirection;
        private readonly List<RemovedTileRecord> _displaced = new();

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

        public bool Changed { get; private set; }

        public void Apply(MapEditorCommandContext ctx)
        {
            bool placed = false;
            DisplacedTiles.Capture(ctx, _displaced, () => placed = Execute(ctx, _position, _direction));
            Changed = placed;
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            // A place the build check refused left the map alone; clearing the slot would delete what blocked it.
            if (!Changed)
                return;

            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is not TileObjectSo tile)
                return;

            // Swap the previous object back in rather than clear-then-place: a clear would cascade
            // and take the wall mounts with it.
            if (!string.IsNullOrEmpty(_previousAssetName)
                && ctx.ResolveAsset(_previousAssetName) is TileObjectSo previousTile)
            {
                ctx.Construction.TryPlaceTile(
                    previousTile, _position, _previousDirection, replaceExisting: true, skipBuildCheck: false);
            }
            else
            {
                ctx.Construction.TryClearTile(_position, tile.layer, _direction);
            }

            DisplacedTiles.Restore(ctx, _displaced);
        }

        private bool Execute(MapEditorCommandContext ctx, Vector3 position, Direction direction)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            return asset is TileObjectSo tile
                && ctx.Construction.TryPlaceTile(tile, position, direction, _replaceExisting, skipBuildCheck: false).Success;
        }
    }

    public sealed class PlaceItemCommand : IMapEditorCommand, IReportsChange
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

        public bool Changed { get; private set; }

        public void Apply(MapEditorCommandContext ctx)
        {
            Changed = false;
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is ItemObjectSo item)
            {
                Quaternion rotation = Quaternion.Euler(0f, TileHelper.GetRotationAngle(_direction), 0f);
                Changed = ctx.Construction.TryPlaceItem(item, _position, rotation).Success;
            }
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            if (!Changed)
                return;

            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is ItemObjectSo item)
                ctx.Construction.TryClearItem(_position, item);
        }
    }

    public sealed class ClearTileCommand : IMapEditorCommand, IReportsChange
    {
        private readonly string _assetName;
        private readonly Vector3 _position;
        private readonly Direction _direction;

        private readonly List<RemovedTileRecord> _displaced = new();

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

        public bool Changed { get; private set; }

        public void Apply(MapEditorCommandContext ctx)
        {
            Changed = false;
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is TileObjectSo tile)
            {
                bool cleared = false;
                DisplacedTiles.Capture(ctx, _displaced,
                    () => cleared = ctx.Construction.TryClearTile(_position, tile.layer, _direction).Success);

                // TryClearTile succeeds on an empty slot too; the recorded removals say whether anything went.
                Changed = cleared && (ctx.Map == null || _displaced.Count > 0);
            }
        }

        /// <summary>
        /// Puts back the cleared object and everything its clear cascaded to (mounts on a wall). Only what this clear
        /// removed: an empty clear puts nothing back, and nothing is placed over whatever is there now.
        /// </summary>
        public void Revert(MapEditorCommandContext ctx)
        {
            if (!Changed)
                return;

            if (_displaced.Count == 0)
            {
                new PlaceTileCommand(_assetName, _position, _direction, replaceExisting: false).Apply(ctx);
                return;
            }

            DisplacedTiles.Restore(ctx, _displaced);
        }
    }

    public sealed class ClearItemCommand : IMapEditorCommand, IReportsChange
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

        public bool Changed { get; private set; }

        public void Apply(MapEditorCommandContext ctx)
        {
            Changed = false;
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is ItemObjectSo item)
                Changed = ctx.Construction.TryClearItem(_position, item).Success;
        }

        /// <summary>Re-places the item only if this clear removed one.</summary>
        public void Revert(MapEditorCommandContext ctx)
        {
            if (Changed)
                new PlaceItemCommand(_assetName, _position, _direction).Apply(ctx);
        }
    }

    public sealed class MoveTileCommand : IMapEditorCommand, IReportsChange
    {
        private readonly string _assetName;
        private readonly Vector3 _from;
        private readonly Vector3 _to;
        private readonly Direction _direction;
        private readonly List<RemovedTileRecord> _displaced = new();

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

        public bool Changed { get; private set; }

        /// <summary>
        /// Checks the destination before it clears the source, so a blocked move loses nothing; if the place still
        /// fails, everything the clear removed goes back.
        /// </summary>
        public void Apply(MapEditorCommandContext ctx)
        {
            bool moved = false;
            DisplacedTiles.Capture(ctx, _displaced, () => moved = TryMove(ctx, _from, _to));
            if (!moved)
                DisplacedTiles.Restore(ctx, _displaced);

            Changed = moved;
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            if (!Changed)
                return;

            TryMove(ctx, _to, _from);
            DisplacedTiles.Restore(ctx, _displaced);
        }

        private bool TryMove(MapEditorCommandContext ctx, Vector3 from, Vector3 to)
        {
            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is not TileObjectSo tile)
                return false;

            if (!ctx.Map.TryGetTileLocation(tile.layer, from, out ITileLocation location)
                || !location.TryGetPlacedObject(out PlacedTileObject placed)
                || placed == null)
            {
                return false;
            }

            Direction direction = placed.Direction;
            if (!ctx.Construction.TryPreviewTile(tile, to, direction, replaceExisting: false).CanBuild)
                return false;

            if (!ctx.Construction.TryClearTile(from, tile.layer, direction).Success)
                return false;

            return ctx.Construction.TryPlaceTile(tile, to, direction, replaceExisting: false, skipBuildCheck: false).Success;
        }
    }

    /// <summary>
    /// Moves a placed item: clears it at <c>from</c>, places it at <c>to</c>, and puts it back if the place fails, so a
    /// refused move loses nothing (tech-debt P2.4).
    /// </summary>
    public sealed class MoveItemCommand : IMapEditorCommand, IReportsChange
    {
        private readonly string _assetName;
        private readonly Vector3 _from;
        private readonly Vector3 _to;
        private readonly Direction _direction;

        public MoveItemCommand(string assetName, Vector3 from, Vector3 to, Direction direction)
        {
            _assetName = assetName;
            _from = from;
            _to = to;
            _direction = direction;
        }

        public bool Changed { get; private set; }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.MoveItem,
                AssetName = _assetName,
                Position = _to,
                PreviousPosition = _from,
                Direction = _direction,
            };

        public void Apply(MapEditorCommandContext ctx) => Changed = TryMove(ctx, _from, _to);

        public void Revert(MapEditorCommandContext ctx)
        {
            if (Changed)
                TryMove(ctx, _to, _from);
        }

        private bool TryMove(MapEditorCommandContext ctx, Vector3 from, Vector3 to)
        {
            if (ctx.ResolveAsset(_assetName) is not ItemObjectSo item)
                return false;

            if (!ctx.Construction.TryClearItem(from, item).Success)
                return false;

            Quaternion rotation = Quaternion.Euler(0f, TileHelper.GetRotationAngle(_direction), 0f);
            if (ctx.Construction.TryPlaceItem(item, to, rotation).Success)
                return true;

            ctx.Construction.TryPlaceItem(item, from, rotation);
            return false;
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

    /// <summary>
    /// Undo support for the clear cascade: a tile command records every object its edit removed,
    /// dependents included, and its revert places back the ones whose slot is still empty.
    /// </summary>
    internal static class DisplacedTiles
    {
        public static void Capture(MapEditorCommandContext ctx, List<RemovedTileRecord> into, Action edit)
        {
            into.Clear();
            if (ctx.Map == null)
            {
                edit();
                return;
            }

            using (ctx.Map.RecordRemovals(into))
                edit();
        }

        public static void Restore(MapEditorCommandContext ctx, List<RemovedTileRecord> records)
        {
            if (ctx.Map == null || ctx.Construction == null || records.Count == 0)
                return;

            // Lower layers first: a plenum before its turf, a wall before its mounts.
            var ordered = new List<RemovedTileRecord>(records);
            ordered.Sort((a, b) => a.Tile.layer.CompareTo(b.Tile.layer));

            foreach (RemovedTileRecord record in ordered)
            {
                if (record.Tile == null)
                    continue;

                // The command's own revert may already have put this slot back.
                if (ctx.Map.TryGetTileLocation(record.Tile.layer, record.Origin, out ITileLocation location)
                    && !location.IsEmpty(record.Direction))
                {
                    continue;
                }

                // These objects stood here before the edit, so skip the build rules: a neighbour
                // rule must not refuse to restore the exact state the map was in.
                ctx.Construction.TryPlaceTile(
                    record.Tile, record.Origin, record.Direction, replaceExisting: false, skipBuildCheck: true);
            }
        }
    }

    public sealed class CompoundCommand : IMapEditorCommand, IReportsChange
    {
        private readonly IMapEditorCommand[] _children;

        public CompoundCommand(IMapEditorCommand[] children) => _children = children;

        /// <summary>Any child changed the map; an empty compound changes nothing but stays an undo step, as before.</summary>
        public bool Changed
        {
            get
            {
                if (_children.Length == 0)
                    return true;

                foreach (IMapEditorCommand child in _children)
                {
                    if (child is not IReportsChange { Changed: false })
                        return true;
                }

                return false;
            }
        }

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
