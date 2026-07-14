using System;

namespace SS3D.Systems.IdAccess
{
    public readonly struct CrewRecordId : IEquatable<CrewRecordId>
    {
        public const uint None = 0;

        public uint Value { get; }

        public CrewRecordId(uint value)
        {
            Value = value;
        }

        public bool IsNone => Value == None;

        public bool Equals(CrewRecordId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is CrewRecordId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => IsNone ? "CrewRecordId.None" : $"CrewRecordId({Value})";

        public static bool operator ==(CrewRecordId left, CrewRecordId right) => left.Equals(right);

        public static bool operator !=(CrewRecordId left, CrewRecordId right) => !left.Equals(right);
    }
}
