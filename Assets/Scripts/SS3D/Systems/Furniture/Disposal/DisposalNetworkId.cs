namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Stable identifier for a connected disposal pipe network component.
    /// </summary>
    public readonly struct DisposalNetworkId
    {
        public const ushort NoneValue = 0;

        public static readonly DisposalNetworkId None = new(NoneValue);

        public readonly ushort Value;

        public bool IsNone => Value == NoneValue;

        public DisposalNetworkId(ushort value) => Value = value;

        public override string ToString() => IsNone ? "None" : Value.ToString();

        public override bool Equals(object obj) => obj is DisposalNetworkId other && Value == other.Value;

        public override int GetHashCode() => Value;
    }
}
