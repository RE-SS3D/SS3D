namespace SS3D.UI.MachineInterface
{
    public struct GasPumpInterfaceSnapshot
    {
        public int MachineObjectId;

        public string InterfaceId;

        public string Title;

        public string ModelLabel;

        public bool PowerOk;

        public bool Enabled;

        public bool Connected;

        public float RatedMaxFlowMolesPerSecond;

        public float CurrentFlowMolesPerSecond;

        public float DifferentialKpa;

        public float MaxDifferentialKpa;

        public bool Stalled;

        /// <summary>0 idle, 1 flowing, 2 stalled, 3 unpowered.</summary>
        public byte HealthState;
    }
}
