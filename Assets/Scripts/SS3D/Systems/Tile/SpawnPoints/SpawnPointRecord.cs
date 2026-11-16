using System;
using UnityEngine;

namespace SS3D.Systems.Tile.SpawnPoints
{
    /// <summary>
    /// One authored spawn marker on a station map. At most one marker per tile
    /// (see <see cref="SpawnPointRegistry"/>).
    /// </summary>
    [Serializable]
    public struct SpawnPointRecord : IEquatable<SpawnPointRecord>
    {
        public SpawnPointKind Kind;
        public string JobName;
        public AntagonistSpawnCategory AntagonistCategory;
        public Vector3 Position;
        public Direction Direction;

        public static SpawnPointRecord ForJob(string jobName, Vector3 position, Direction direction) =>
            new()
            {
                Kind = SpawnPointKind.Job,
                JobName = jobName ?? string.Empty,
                Position = position,
                Direction = direction,
            };

        public static SpawnPointRecord ForAntagonist(
            AntagonistSpawnCategory category, Vector3 position, Direction direction) =>
            new()
            {
                Kind = SpawnPointKind.Antagonist,
                JobName = string.Empty,
                AntagonistCategory = category,
                Position = position,
                Direction = direction,
            };

        public bool Equals(SpawnPointRecord other) =>
            Kind == other.Kind
            && AntagonistCategory == other.AntagonistCategory
            && Direction == other.Direction
            && Position == other.Position
            && string.Equals(JobName, other.JobName, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SpawnPointRecord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (JobName?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ (int)AntagonistCategory;
                hash = (hash * 397) ^ Position.GetHashCode();
                hash = (hash * 397) ^ (int)Direction;
                return hash;
            }
        }
    }
}
