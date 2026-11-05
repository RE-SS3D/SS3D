using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>Authoritative tile membership edits. Geometry never repartitions an authored map.</summary>
    public sealed class AreaPaintingService
    {
        private readonly TileMap _map;
        private readonly ITileQueryService _query;
        private readonly AreaRegistry _registry;
        private readonly IReadOnlyList<IAreaApcOrigin> _apcs;
        public bool HasAuthoredAreas { get; set; }
        public event Action Changed;

        public AreaPaintingService(TileMap map, ITileQueryService query, AreaRegistry registry, IReadOnlyList<IAreaApcOrigin> apcs)
        {
            _map = map;
            _query = query;
            _registry = registry;
            _apcs = apcs;
        }

        public SavedAreaRecord[] CaptureRecords()
        {
            List<SavedAreaRecord> result = new();
            foreach (AreaRecord room in _registry.GetAllAreas())
            {
                if (room.Apc == null && !HasAuthoredAreas) continue;
                result.Add(new SavedAreaRecord
                {
                    id = room.Id.Value, authored = HasAuthoredAreas, noApc = room.Apc == null,
                    displayName = room.DisplayName, parentTag = room.ParentTag,
                    ambienceProfileId = room.AmbienceProfileId,
                    apcWorldPosition = room.Apc != null ? _query.TileToWorld(room.Apc.OriginTile) : Vector3.zero,
                    hasDepartmentalLightTint = room.HasDepartmentalLightTint,
                    departmentalLightTint = room.DepartmentalLightTint,
                    defaultRequiredAccessBits = room.DefaultRequiredAccess.Value,
                    lightingSwitchOn = room.LightingSwitchOn,
                });
            }
            return result.ToArray();
        }

        public void RestoreRecords(IReadOnlyList<SavedAreaRecord> saved, bool authored = false)
        {
            _registry.Clear();
            HasAuthoredAreas = authored || (saved != null && saved.Any(room => room.authored));
            if (saved == null) return;
            foreach (SavedAreaRecord room in saved)
            {
                if (room.id == AreaId.None) continue;
                _registry.Register(new AreaRecord
                {
                    Id = new AreaId(room.id), DisplayName = room.displayName,
                    ParentTag = AreaDepartments.Normalize(room.parentTag),
                    AmbienceProfileId = room.ambienceProfileId,
                    HasDepartmentalLightTint = room.hasDepartmentalLightTint,
                    DepartmentalLightTint = room.departmentalLightTint,
                    DefaultRequiredAccess = new IdAccess.AccessMask(room.defaultRequiredAccessBits),
                    LightingSwitchOn = room.lightingSwitchOn,
                });
                _registry.EnsureNextIdAbove(room.id);
            }
            if (HasAuthoredAreas)
            {
                HashSet<ushort> used = new();
                foreach (TileChunk chunk in _map.GetAllChunks())
                    for (int y = 0; y < TileConstants.ChunkSize; y++)
                        for (int x = 0; x < TileConstants.ChunkSize; x++)
                        {
                            ushort id = chunk.GetAreaId(x, y);
                            TileCoord coord = _query.WorldToTile(chunk.GetWorldPosition(x, y), _map.MapId);
                            if (id != AreaId.None && AreaBoundaryEvaluator.IsTurfWall(_query, coord))
                                chunk.SetAreaId(x, y, AreaId.None);
                            else used.Add(id);
                        }
                foreach (AreaRecord room in _registry.GetAllAreas().ToArray())
                    if (!used.Contains(room.Id.Value)) _registry.Unregister(room.Id);
                RefreshApcLinks();
            }
        }

        public bool TryPaint(ushort target, IReadOnlyList<Vector2Int> tiles, out AreaPaintChange change, out string error)
        {
            change = null;
            error = null;
            if (target != AreaId.None && !_registry.TryGet(new AreaId(target), out _))
            { error = "Select an existing room first."; return false; }
            if (tiles == null || tiles.Count == 0 || tiles.Count > 4096)
            { error = "Paint between 1 and 4096 tiles per stroke."; return false; }
            Dictionary<Vector2Int, ushort> before = new();
            Dictionary<Vector2Int, ushort> after = new();
            foreach (Vector2Int tile in tiles)
            {
                TileCoord coord = new(_map.MapId, tile);
                if (!AreaBoundaryEvaluator.IsWalkable(_query, coord)) continue;
                _map.TryGetAreaId(coord, out ushort old);
                if (old == target) continue;
                before[tile] = old;
                after[tile] = target;
            }
            if (after.Count == 0) { error = "No room tiles changed. Paint floor or door tiles."; return false; }
            HashSet<ushort> occupied = new();
            foreach (IAreaApcOrigin apc in _apcs)
            {
                if (!IsLive(apc)) continue;
                TileCoord coord = GetApcMembershipTile(apc);
                ushort id;
                if (!after.TryGetValue(coord.Grid, out id)) _map.TryGetAreaId(coord, out id);
                if (id != AreaId.None && !occupied.Add(id))
                { error = "A room can have only one APC. Paint the APCs into separate rooms."; return false; }
            }
            // Only retire rooms affected by this stroke. Keep partial rooms and the paint target.
            HashSet<ushort> candidates = new(before.Values.Where(id => id != AreaId.None && id != target));
            Dictionary<ushort, List<Vector2Int>> remaining = new();
            HashSet<ushort> roomsWithFloor = new();
            foreach (TileChunk chunk in _map.GetAllChunks())
                for (int y = 0; y < TileConstants.ChunkSize; y++)
                    for (int x = 0; x < TileConstants.ChunkSize; x++)
                    {
                        TileCoord coord = _query.WorldToTile(chunk.GetWorldPosition(x, y), _map.MapId);
                        ushort id = after.TryGetValue(coord.Grid, out ushort next) ? next : chunk.GetAreaId(x, y);
                        if (!candidates.Contains(id)) continue;
                        if (!remaining.TryGetValue(id, out List<Vector2Int> list)) remaining[id] = list = new();
                        list.Add(coord.Grid);
                        if (AreaBoundaryEvaluator.IsWalkable(_query, coord)) roomsWithFloor.Add(id);
                    }
            List<AreaRecord> removed = new();
            foreach (ushort id in candidates)
            {
                if (roomsWithFloor.Contains(id) || !_registry.TryGet(new AreaId(id), out AreaRecord room)) continue;
                removed.Add(room);
                // APC origin tiles on walls may still carry the old default id; clear those too.
                if (remaining.TryGetValue(id, out List<Vector2Int> residual))
                    foreach (Vector2Int tile in residual) { before[tile] = id; after[tile] = AreaId.None; }
            }
            change = new AreaPaintChange(this, before, after, HasAuthoredAreas, removed);
            change.Redo();
            return true;
        }

        /// <summary>A despawned APC is Unity-null but still a live C# reference; it must not claim a room.</summary>
        private static bool IsLive(IAreaApcOrigin apc) => apc is not UnityEngine.Object unityObject || unityObject;

        public TileCoord GetApcMembershipTile(IAreaApcOrigin apc) => AreaDeviceTileResolver.GetApcRoomTile(apc, _query);

        public string NextRoomName()
        {
            HashSet<string> names = new(_registry.GetAllAreas().Select(room => room.DisplayName?.Trim() ?? ""), StringComparer.OrdinalIgnoreCase);
            int number = 1;
            while (names.Contains($"Room {number}")) number++;
            return $"Room {number}";
        }

        public void RefreshApcLinks()
        {
            foreach (AreaRecord record in _registry.GetAllAreas()) record.Apc = null;
            foreach (IAreaApcOrigin apc in _apcs) if (IsLive(apc)) apc.SetMultipleApcsInArea(false);
            foreach (IAreaApcOrigin apc in _apcs)
            {
                if (!IsLive(apc) || !_map.TryGetAreaId(GetApcMembershipTile(apc), out ushort id) ||
                    !_registry.TryGet(new AreaId(id), out AreaRecord record)) continue;
                if (record.Apc != null)
                {
                    record.Apc.SetMultipleApcsInArea(true);
                    apc.SetMultipleApcsInArea(true);
                }
                else record.Apc = apc;
            }
            _registry.RefreshApcLookup();
        }

        public void NotifyChanged()
        {
            RefreshApcLinks();
            Changed?.Invoke();
        }

        private void SetMembership(IReadOnlyDictionary<Vector2Int, ushort> values, bool authored, IReadOnlyList<AreaRecord> removed, bool undo)
        {
            if (undo) foreach (AreaRecord room in removed) _registry.Register(room);
            foreach (KeyValuePair<Vector2Int, ushort> pair in values)
                _map.TrySetAreaId(new TileCoord(_map.MapId, pair.Key), pair.Value);
            if (!undo) foreach (AreaRecord room in removed) _registry.Unregister(room.Id);
            HasAuthoredAreas = authored;
            NotifyChanged();
        }

        public sealed class AreaPaintChange
        {
            private readonly AreaPaintingService _owner;
            private readonly Dictionary<Vector2Int, ushort> _before, _after;
            private readonly bool _wasAuthored;
            private readonly IReadOnlyList<AreaRecord> _removed;
            internal AreaPaintChange(AreaPaintingService owner, Dictionary<Vector2Int, ushort> before,
                Dictionary<Vector2Int, ushort> after, bool wasAuthored, IReadOnlyList<AreaRecord> removed)
            { _owner = owner; _before = before; _after = after; _wasAuthored = wasAuthored; _removed = removed; }
            public void Undo() => _owner.SetMembership(_before, _wasAuthored, _removed, true);
            public void Redo() => _owner.SetMembership(_after, true, _removed, false);
        }
    }
}
