namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Stable identifier for a connected gas pipe network component.
    /// </summary>
    public readonly struct GasPipeNetworkId
    {
        public const ushort NoneValue = 0;

        public static readonly GasPipeNetworkId None = new(NoneValue);

        public readonly ushort Value;

        public bool IsNone => Value == NoneValue;

        public GasPipeNetworkId(ushort value) => Value = value;

        public override string ToString() => IsNone ? "None" : Value.ToString();

        public override bool Equals(object obj) => obj is GasPipeNetworkId other && Value == other.Value;

        public override int GetHashCode() => Value;
    }
}
