using System;
using JetBrains.Annotations;

namespace SS3D.Data
{
    /// <summary>
    /// Identifies an asset inside an SS3D asset database.
    /// This is the canonical domain key for systems that need both the database ID and the asset ID together.
    /// </summary>
    [Serializable]
    public readonly struct AssetKey : IEquatable<AssetKey>
    {
        /// <summary>
        /// ID of the asset database that owns the asset.
        /// </summary>
        public readonly string DatabaseId;

        /// <summary>
        /// ID of the asset inside the owning database.
        /// </summary>
        public readonly string AssetId;

        public AssetKey([CanBeNull] string databaseId, [CanBeNull] string assetId)
        {
            DatabaseId = databaseId;
            AssetId = assetId;
        }

        /// <summary>
        /// Returns <see langword="true"/> when both parts of the key are present and non-whitespace.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(DatabaseId) && !string.IsNullOrWhiteSpace(AssetId);

        public static bool operator ==(AssetKey left, AssetKey right) => left.Equals(right);

        public static bool operator !=(AssetKey left, AssetKey right) => !left.Equals(right);

        public void Deconstruct([CanBeNull] out string databaseId, [CanBeNull] out string assetId)
        {
            databaseId = DatabaseId;
            assetId = AssetId;
        }

        public bool Equals(AssetKey other) => string.Equals(DatabaseId, other.DatabaseId, StringComparison.Ordinal) &&
            string.Equals(AssetId, other.AssetId, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is AssetKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(DatabaseId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(AssetId ?? string.Empty);

                return hashCode;
            }
        }

        [NotNull]
        public override string ToString() => $"{DatabaseId}/{AssetId}";
    }
}
