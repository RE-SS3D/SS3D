using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.SpawnPoints
{
    /// <summary>
    /// In-memory spawn markers for the loaded station map. Multiple markers may share a
    /// job/antag tag; at most one marker occupies a given tile cell.
    /// </summary>
    public sealed class SpawnPointRegistry
    {
        private readonly List<SpawnPointRecord> _records = new();
        private readonly Dictionary<Vector2Int, int> _indexByTile = new();

        public event Action Changed;

        public IReadOnlyList<SpawnPointRecord> Records => _records;

        public int Count => _records.Count;

        public static Vector2Int TileKey(Vector3 worldPosition)
        {
            Vector3 snapped = TileHelper.GetClosestPosition(worldPosition);
            return new Vector2Int(Mathf.RoundToInt(snapped.x), Mathf.RoundToInt(snapped.z));
        }

        public bool TryGetAt(Vector3 worldPosition, out SpawnPointRecord record)
        {
            Vector2Int key = TileKey(worldPosition);
            if (_indexByTile.TryGetValue(key, out int index))
            {
                record = _records[index];
                return true;
            }

            record = default;
            return false;
        }

        public bool HasAt(Vector3 worldPosition) => _indexByTile.ContainsKey(TileKey(worldPosition));

        /// <summary>
        /// Position of the <paramref name="turn"/>-th marker for <paramref name="jobName"/> (wrapping), so
        /// players of one job spread over its markers. Job names match ordinally, as Map Check does.
        /// Returns false when the job has no marker.
        /// </summary>
        public bool TryGetJobSpawn(string jobName, int turn, out Vector3 position)
        {
            position = default;
            if (string.IsNullOrEmpty(jobName))
                return false;

            int count = 0;
            foreach (SpawnPointRecord record in _records)
            {
                if (IsJobMarker(record, jobName))
                    count++;
            }

            if (count == 0)
                return false;

            int target = ((turn % count) + count) % count;
            foreach (SpawnPointRecord record in _records)
            {
                if (!IsJobMarker(record, jobName))
                    continue;

                if (target-- == 0)
                {
                    position = record.Position;
                    return true;
                }
            }

            return false;
        }

        private static bool IsJobMarker(SpawnPointRecord record, string jobName) =>
            record.Kind == SpawnPointKind.Job && string.Equals(record.JobName, jobName, StringComparison.Ordinal);

        /// <summary>
        /// Places or replaces the marker on the snapped tile. Returns false if
        /// <paramref name="record"/> is invalid.
        /// </summary>
        public bool TryPlace(SpawnPointRecord record)
        {
            if (record.Kind == SpawnPointKind.Job && string.IsNullOrWhiteSpace(record.JobName))
                return false;

            Vector3 snapped = TileHelper.GetClosestPosition(record.Position);
            record.Position = snapped;
            Vector2Int key = TileKey(snapped);

            if (_indexByTile.TryGetValue(key, out int existing))
            {
                _records[existing] = record;
            }
            else
            {
                _indexByTile[key] = _records.Count;
                _records.Add(record);
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes the marker at the snapped tile. Outputs the previous record when present.
        /// </summary>
        public bool TryClearAt(Vector3 worldPosition, out SpawnPointRecord previous)
        {
            Vector2Int key = TileKey(worldPosition);
            if (!_indexByTile.TryGetValue(key, out int index))
            {
                previous = default;
                return false;
            }

            previous = _records[index];
            RemoveAtIndex(index, key);
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (_records.Count == 0)
                return;

            _records.Clear();
            _indexByTile.Clear();
            Changed?.Invoke();
        }

        public void ReplaceAll(IReadOnlyList<SpawnPointRecord> records)
        {
            _records.Clear();
            _indexByTile.Clear();

            if (records != null)
            {
                foreach (SpawnPointRecord record in records)
                {
                    if (record.Kind == SpawnPointKind.Job && string.IsNullOrWhiteSpace(record.JobName))
                        continue;

                    Vector3 snapped = TileHelper.GetClosestPosition(record.Position);
                    SpawnPointRecord placed = record;
                    placed.Position = snapped;
                    Vector2Int key = TileKey(snapped);

                    if (_indexByTile.TryGetValue(key, out int existing))
                        _records[existing] = placed;
                    else
                    {
                        _indexByTile[key] = _records.Count;
                        _records.Add(placed);
                    }
                }
            }

            Changed?.Invoke();
        }

        private void RemoveAtIndex(int index, Vector2Int key)
        {
            int last = _records.Count - 1;
            if (index < last)
            {
                SpawnPointRecord moved = _records[last];
                _records[index] = moved;
                _indexByTile[TileKey(moved.Position)] = index;
            }

            _records.RemoveAt(last);
            _indexByTile.Remove(key);
        }
    }
}
