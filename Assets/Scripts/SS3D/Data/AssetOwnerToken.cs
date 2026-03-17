using JetBrains.Annotations;
using System;

namespace SS3D.Data
{
    /// <summary>
    /// Identifies one logical owner of a loaded addressable asset residency claim.
    /// Tokens are value types backed by a unique identifier, so callers can copy and
    /// store them safely while still referring to the same ownership record.
    /// </summary>
    [Serializable]
    public readonly struct AssetOwnerToken : IEquatable<AssetOwnerToken>
    {
        private readonly Guid _id;

        private AssetOwnerToken(Guid id, [CanBeNull] string name)
        {
            _id = id;
            Name = name;
        }

        /// <summary>
        /// Human-readable label used for diagnostics.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns <see langword="true"/> when this token represents a real ownership claim.
        /// </summary>
        public bool IsValid => _id != Guid.Empty;

        public static bool operator ==(AssetOwnerToken left, AssetOwnerToken right) => left.Equals(right);

        public static bool operator !=(AssetOwnerToken left, AssetOwnerToken right) => !left.Equals(right);

        /// <summary>
        /// Creates a new ownership token for one logical asset owner.
        /// </summary>
        /// <exception cref="ArgumentException">If name is null or whitespace.</exception>
        public static AssetOwnerToken Create([NotNull] string name) => string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Asset owner token name cannot be null or whitespace.", nameof(name))
            : new(Guid.NewGuid(), name);

        public bool Equals(AssetOwnerToken other) => _id.Equals(other._id);

        public override bool Equals(object obj) => obj is AssetOwnerToken other && Equals(other);

        public override int GetHashCode() => _id.GetHashCode();

        [NotNull]
        public override string ToString() => IsValid ? Name ?? _id.ToString() : "<invalid-owner>";
    }
}