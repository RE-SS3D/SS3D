namespace SS3D.UI.MachineInterface
{
    public sealed class GasPumpInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; init; }

        public string ModelLabel { get; init; }

        public bool PowerOk { get; init; }

        public bool Enabled { get; init; }

        public bool Connected { get; init; }

        public float RatedMaxFlowMolesPerSecond { get; init; }

        public float CurrentFlowMolesPerSecond { get; init; }

        public float DifferentialKpa { get; init; }

        public float MaxDifferentialKpa { get; init; }

        public bool Stalled { get; init; }

        public byte HealthState { get; init; }

        public string FlowReadout { get; init; }

        public string DifferentialReadout { get; init; }

        public string StatusReadout { get; init; }

        public StatusTone StatusTone { get; init; }
    }
}
