using System;

namespace SS3D.Systems.IdAccess
{
    public readonly struct AccessMask : IEquatable<AccessMask>
    {
        public static readonly AccessMask None = new AccessMask(0UL);

        public ulong Value { get; }

        public AccessMask(ulong value)
        {
            Value = value;
        }

        public AccessMask(AccessLevel level) : this((ulong)level)
        {
        }

        public bool IsNone => Value == 0;

        public bool HasAll(AccessMask required) => required.IsNone || (Value & required.Value) == required.Value;

        public bool HasAny(AccessMask required) => required.IsNone || (Value & required.Value) != 0;

        public AccessMask With(AccessLevel level) => new(Value | (ulong)level);

        public AccessMask Without(AccessLevel level) => new(Value & ~(ulong)level);

        public bool Equals(AccessMask other) => Value == other.Value;

        public override bool Equals(object obj) => obj is AccessMask other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => IsNone ? "AccessMask.None" : $"AccessMask(0x{Value:X})";

        public static AccessMask operator |(AccessMask left, AccessMask right) => new(left.Value | right.Value);

        public static AccessMask operator &(AccessMask left, AccessMask right) => new(left.Value & right.Value);

        public static bool operator ==(AccessMask left, AccessMask right) => left.Equals(right);

        public static bool operator !=(AccessMask left, AccessMask right) => !left.Equals(right);

        public static AccessMask FromLevels(params AccessLevel[] levels)
        {
            ulong value = 0;
            foreach (AccessLevel level in levels)
            {
                value |= (ulong)level;
            }

            return new AccessMask(value);
        }
    }
}
