using System;
using UnityEngine;

namespace SS3D.Systems.Tile.SpawnPoints
{
    /// <summary>
    /// JsonUtility-friendly DTO for one spawn marker in a station template.
    /// </summary>
    [Serializable]
    public sealed class SavedSpawnPointRecord
    {
        public SpawnPointKind kind;
        public string jobName;
        public AntagonistSpawnCategory antagonistCategory;
        public Vector3 position;
        public Direction direction;

        public SpawnPointRecord ToRecord() =>
            new()
            {
                Kind = kind,
                JobName = jobName ?? string.Empty,
                AntagonistCategory = antagonistCategory,
                Position = position,
                Direction = direction,
            };

        public static SavedSpawnPointRecord FromRecord(SpawnPointRecord record) =>
            new()
            {
                kind = record.Kind,
                jobName = record.JobName ?? string.Empty,
                antagonistCategory = record.AntagonistCategory,
                position = record.Position,
                direction = record.Direction,
            };
    }

    [Serializable]
    public sealed class SavedSpawnPointChunkPayload
    {
        public SavedSpawnPointRecord[] records;
    }
}
