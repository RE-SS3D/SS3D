using SS3D.Systems.Tile.SpawnPoints;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.GasMarkers
{
    /// <summary>
    /// In-memory gas markers for the loaded station map, one per tile. The server owns them;
    /// <see cref="TileSubSystem"/> copies them to clients for the editor's pins.
    /// </summary>
    public sealed class GasMarkerRegistry
    {
        private readonly Dictionary<Vector2Int, GasMarkerRecord> _byTile = new();

        public event Action Changed;

        public IReadOnlyCollection<GasMarkerRecord> Records => _byTile.Values;

        public int Count => _byTile.Count;

        public bool TryGetAt(Vector3 worldPosition, out GasMarkerRecord record) =>
            _byTile.TryGetValue(SpawnPointRegistry.TileKey(worldPosition), out record);

        public bool HasAt(Vector3 worldPosition) => _byTile.ContainsKey(SpawnPointRegistry.TileKey(worldPosition));

        /// <summary>Places or replaces the marker on the snapped tile. Returns false if the record is invalid.</summary>
        public bool TryPlace(GasMarkerRecord record)
        {
            if (!Store(record))
                return false;

            Changed?.Invoke();
            return true;
        }

        /// <summary>Removes the marker at the snapped tile. Outputs the previous record when present.</summary>
        public bool TryClearAt(Vector3 worldPosition, out GasMarkerRecord previous)
        {
            if (!_byTile.Remove(SpawnPointRegistry.TileKey(worldPosition), out previous))
                return false;

            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (_byTile.Count == 0)
                return;

            _byTile.Clear();
            Changed?.Invoke();
        }

        public void ReplaceAll(IReadOnlyList<GasMarkerRecord> records)
        {
            _byTile.Clear();
            if (records != null)
            {
                foreach (GasMarkerRecord record in records)
                    Store(record);
            }

            Changed?.Invoke();
        }

        private bool Store(GasMarkerRecord record)
        {
            if (!record.IsValid)
                return false;

            record.Position = TileHelper.GetClosestPosition(record.Position);
            _byTile[SpawnPointRegistry.TileKey(record.Position)] = record;
            return true;
        }
    }
}
