using System;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Client/host cache of area membership and departmental tints for floor stripe rendering.
    /// Host populates from live tilemap data; pure clients receive ObserversRpc snapshots.
    /// </summary>
    public sealed class AreaFloorVisualCache
    {
        public event Action OnDirty;

        private readonly Dictionary<ushort, string> _names = new();

        public IReadOnlyDictionary<ushort, string> AreaNames => _names;

        public string GetAreaName(ushort id) => _names.TryGetValue(id, out string name) && !string.IsNullOrWhiteSpace(name) ? name : $"Area {id}";

        public void ReplaceNames(ushort[] ids, string[] names)
        {
            _names.Clear();
            if (ids != null && names != null)
                for (int i = 0; i < Math.Min(ids.Length, names.Length); i++) _names[ids[i]] = names[i];
            OnDirty?.Invoke();
        }

        private readonly Dictionary<ushort, Color> _tints = new();
        private readonly Dictionary<Vector2Int, ushort[]> _chunkAreaIds = new();
        private readonly Dictionary<ushort, string> _ambienceProfileIds = new();

        public void Clear()
        {
            _names.Clear();
            _tints.Clear();
            _chunkAreaIds.Clear();
            _ambienceProfileIds.Clear();
            OnDirty?.Invoke();
        }

        public void SetTint(ushort areaId, bool hasTint, Color tint)
        {
            if (areaId == AreaId.None)
                return;

            if (hasTint)
                _tints[areaId] = tint;
            else
                _tints.Remove(areaId);
        }

        public void SetChunkAreaIds(Vector2Int chunkKey, ushort[] areaIds)
        {
            if (areaIds == null || areaIds.Length == 0)
            {
                _chunkAreaIds.Remove(chunkKey);
                return;
            }

            var copy = new ushort[areaIds.Length];
            Array.Copy(areaIds, copy, areaIds.Length);
            _chunkAreaIds[chunkKey] = copy;
        }

        public void ReplaceAll(
            IReadOnlyList<(ushort areaId, bool hasTint, Color tint)> tints,
            IReadOnlyList<(Vector2Int chunkKey, ushort[] areaIds)> chunks)
        {
            _tints.Clear();
            _chunkAreaIds.Clear();

            if (tints != null)
            {
                foreach ((ushort areaId, bool hasTint, Color tint) in tints)
                    SetTint(areaId, hasTint, tint);
            }

            if (chunks != null)
            {
                foreach ((Vector2Int chunkKey, ushort[] areaIds) in chunks)
                    SetChunkAreaIds(chunkKey, areaIds);
            }

            OnDirty?.Invoke();
        }

        public void NotifyDirty() => OnDirty?.Invoke();

        public bool TryGetTint(ushort areaId, out Color tint) => _tints.TryGetValue(areaId, out tint);

        /// <summary>
        /// Server-populated (host) or ObserversRpc-synced (client) per-area ambience profile id
        /// (audio.md §2 / <c>AreaRecord.AmbienceProfileId</c>). Separate broadcast channel from
        /// tint/chunk data so it can be updated (e.g. on ambience authoring) without touching the
        /// floor-stripe snapshot.
        /// </summary>
        public void SetAmbienceProfileId(ushort areaId, string profileId)
        {
            if (areaId == AreaId.None)
                return;

            if (string.IsNullOrEmpty(profileId))
                _ambienceProfileIds.Remove(areaId);
            else
                _ambienceProfileIds[areaId] = profileId;
        }

        public bool TryGetAmbienceProfileId(ushort areaId, out string profileId) =>
            _ambienceProfileIds.TryGetValue(areaId, out profileId);

        public void ReplaceAmbienceProfileIds(IReadOnlyList<(ushort areaId, string profileId)> profiles)
        {
            _ambienceProfileIds.Clear();

            if (profiles != null)
            {
                foreach ((ushort areaId, string profileId) in profiles)
                    SetAmbienceProfileId(areaId, profileId);
            }

            OnDirty?.Invoke();
        }

        public bool TryGetAreaId(Vector2Int chunkKey, int localX, int localY, out ushort areaId)
        {
            areaId = AreaId.None;
            if (!_chunkAreaIds.TryGetValue(chunkKey, out ushort[] ids))
                return false;

            int index = localY * TileConstants.ChunkSize + localX;
            if (index < 0 || index >= ids.Length)
                return false;

            areaId = ids[index];
            return true;
        }

        /// <summary>
        /// Resolves a world-grid tile to its synced area id (chunk key math matches <c>TileMap.GetKey</c>).
        /// </summary>
        public bool TryGetAreaIdForWorldGrid(Vector2Int worldGrid, out ushort areaId)
        {
            areaId = AreaId.None;
            int size = TileConstants.ChunkSize;
            int chunkX = (int)Math.Floor(worldGrid.x / (double)size);
            int chunkY = (int)Math.Floor(worldGrid.y / (double)size);
            int localX = worldGrid.x - chunkX * size;
            int localY = worldGrid.y - chunkY * size;

            if (!TryGetAreaId(new Vector2Int(chunkX, chunkY), localX, localY, out areaId))
                return false;

            return areaId != AreaId.None;
        }

        public IEnumerable<KeyValuePair<Vector2Int, ushort[]>> EnumerateChunks() => _chunkAreaIds;
    }
}
