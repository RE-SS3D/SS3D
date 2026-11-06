using FishNet.Object;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Combines two inlets at a set ratio up to a target outlet pressure (design §6). It stops if an
    /// inlet it needs runs dry, so a bad mix can't slip out. Inlet 1 is behind it, inlet 2 at its
    /// side port.
    /// </summary>
    public sealed class AtmosMixerController : AtmosThreeWayDeviceBase
    {
        private const ulong AllGases = ulong.MaxValue;

        private int _inlet1Percent = AtmosPortConstants.MixerDefaultInlet1Percent;
        private int _targetOutletPressureKpa = AtmosPortConstants.PumpDefaultTargetKpa;

        public int Inlet1Percent => _inlet1Percent;

        public int TargetOutletPressureKpa => _targetOutletPressureKpa;

        public bool LastInlet1Dry { get; private set; }

        public bool LastInlet2Dry { get; private set; }

        /// <summary>
        /// An inlet is dry if the ratio asks it for gas and it has none. At 100% the other inlet
        /// doesn't matter.
        /// </summary>
        public static bool IsDry(float inletMoles, float share) =>
            share > 0f && inletMoles <= AtmosPortConstants.MixerDryMoles;

        /// <summary>
        /// Total moles to pass this tick, so that both inlets can give their share.
        /// </summary>
        public static float ComputeMoles(
            float inlet1Moles,
            float inlet2Moles,
            float inlet1Share,
            float molesToTarget,
            float deltaTime)
        {
            float moles = Mathf.Min(molesToTarget, AtmosPortConstants.MixerRatedFlowMolesPerSecond * deltaTime);
            float inlet2Share = 1f - inlet1Share;

            if (inlet1Share > 0f)
                moles = Mathf.Min(moles, inlet1Moles / inlet1Share);

            if (inlet2Share > 0f)
                moles = Mathf.Min(moles, inlet2Moles / inlet2Share);

            return Mathf.Max(0f, moles);
        }

        [Server]
        public void ServerSetInlet1Percent(int percent)
        {
            _inlet1Percent = Mathf.Clamp(percent, 0, 100);
        }

        [Server]
        public void ServerSetTargetOutletPressureKpa(int targetKpa)
        {
            _targetOutletPressureKpa = Mathf.Clamp(targetKpa, 0, AtmosPortConstants.MaxTargetPressureKpa);
        }

        /// <summary>
        /// One tick's work, split out so tests can drive it without a tile map.
        /// </summary>
        public float Mix(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord inlet1,
            GasPipeNetworkRecord outlet,
            GasPipeNetworkRecord inlet2,
            float deltaTime)
        {
            float inlet1Share = _inlet1Percent / 100f;
            float inlet1Moles = inlet1.GetTotalMoles(AtmosConstants.DefaultGasCount);
            float inlet2Moles = inlet2.GetTotalMoles(AtmosConstants.DefaultGasCount);

            LastInlet1Dry = IsDry(inlet1Moles, inlet1Share);
            LastInlet2Dry = IsDry(inlet2Moles, 1f - inlet1Share);
            LastStalled = LastInlet1Dry || LastInlet2Dry;

            float outletPressure = outlet.GetPressure(AtmosConstants.DefaultGasCount);
            if (LastStalled || !AtmosPumpController.ShouldPumpToOutlet(outletPressure, _targetOutletPressureKpa))
                return 0f;

            float moles = ComputeMoles(
                inlet1Moles,
                inlet2Moles,
                inlet1Share,
                AtmosPortFlow.ComputeMolesToTarget(outletPressure, _targetOutletPressureKpa, outlet.Temperature, outlet.Volume),
                deltaTime);

            return MoveShare(registry, inlet1, outlet, moles * inlet1Share, inlet1Moles)
                + MoveShare(registry, inlet2, outlet, moles * (1f - inlet1Share), inlet2Moles);
        }

        protected override bool RunThreeWayTick(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord back,
            GasPipeNetworkRecord front,
            GasPipeNetworkRecord side,
            float deltaTime) =>
            RecordFlow(Mix(registry, back, front, side, deltaTime), deltaTime);

        private static float MoveShare(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord inlet,
            GasPipeNetworkRecord outlet,
            float moles,
            float inletMoles) =>
            inletMoles > 0f ? registry.MoveGases(inlet, outlet, AllGases, moles / inletMoles) : 0f;
    }
}
