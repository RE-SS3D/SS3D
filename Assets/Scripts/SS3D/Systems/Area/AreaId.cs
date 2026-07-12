using System;

namespace SS3D.Systems.Area
{
    public readonly struct AreaId : IEquatable<AreaId>
    {
        public const ushort None = 0;

        public ushort Value { get; }

        public AreaId(ushort value)
        {
            Value = value;
        }

        public bool IsNone => Value == None;

        public bool Equals(AreaId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is AreaId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => IsNone ? "AreaId.None" : $"AreaId({Value})";

        public static bool operator ==(AreaId left, AreaId right) => left.Equals(right);

        public static bool operator !=(AreaId left, AreaId right) => !left.Equals(right);
    }
}
