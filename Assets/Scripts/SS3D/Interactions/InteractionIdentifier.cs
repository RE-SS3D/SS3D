using System;

namespace SS3D.Interactions
{
    /// <summary>
    /// Stable wire identifier for an interaction. Display names from <see cref="Interfaces.IInteraction.GetName"/> are not used on the network.
    /// </summary>
    public readonly struct InteractionIdentifier : IEquatable<InteractionIdentifier>
    {
        public const int SourceOnlyTargetIndex = -1;
        public const int SyntheticTargetIndex = -2;

        public string GenericName { get; }
        public int TargetComponentIndex { get; }

        public InteractionIdentifier(string genericName, int targetComponentIndex)
        {
            GenericName = genericName;
            TargetComponentIndex = targetComponentIndex;
        }

        public bool Equals(InteractionIdentifier other)
        {
            return TargetComponentIndex == other.TargetComponentIndex
                && string.Equals(GenericName, other.GenericName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is InteractionIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GenericName, TargetComponentIndex);
        }

        public static bool operator ==(InteractionIdentifier left, InteractionIdentifier right) => left.Equals(right);

        public static bool operator !=(InteractionIdentifier left, InteractionIdentifier right) => !left.Equals(right);
    }
}
