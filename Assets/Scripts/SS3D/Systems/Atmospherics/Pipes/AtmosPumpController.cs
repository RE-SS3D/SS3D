using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Audio;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Inline pump (design §7): moves gas from the network behind it to the one in front, towards a
    /// target outlet pressure or at a set volume. Not area-linked.
    /// </summary>
    public sealed class AtmosPumpController : AtmosInlineDeviceBase, IAmbientLoopState
    {
        private static readonly int PumpActiveId = Animator.StringToHash("pumpActive");

        [SerializeField]
        private PumpRegulation _pumpRegulation = PumpRegulation.OutletPressure;

        [SerializeField]
        private float _ratedMaxFlowMoles = AtmosPortConstants.PumpRatedFlowMolesPerSecond;

        [SerializeField]
        private float _maxDifferentialKpa = AtmosPortConstants.PumpMaxDifferentialKpa;

        [SyncVar]
        private int _targetOutletPressureKpa = AtmosPortConstants.PumpDefaultTargetKpa;

        [SyncVar]
        private int _targetLitresPerSecond = AtmosPortConstants.VolumePumpDefaultLitresPerSecond;

        public override PumpRegulation Regulation => _pumpRegulation;

        public float RatedMaxFlowMolesPerSecond =>
            _ratedMaxFlowMoles > 0f ? _ratedMaxFlowMoles : AtmosPortConstants.PumpRatedFlowMolesPerSecond;

        public float MaxDifferentialKpa =>
            _maxDifferentialKpa > 0f ? _maxDifferentialKpa : AtmosPortConstants.PumpMaxDifferentialKpa;

        public int TargetOutletPressureKpa => _targetOutletPressureKpa;

        public int TargetLitresPerSecond => _targetLitresPerSecond;

        public static bool ShouldPumpToOutlet(float outletPressureKpa, int targetOutletPressureKpa) =>
            targetOutletPressureKpa > 0 && outletPressureKpa < targetOutletPressureKpa;

        /// <summary>
        /// Moles a pressure pump moves this tick: the pump curve, stopping at the target.
        /// </summary>
        public static float ComputePressureMoles(
            float inletPressureKpa,
            GasPipeNetworkRecord outlet,
            int targetOutletPressureKpa,
            float ratedMaxFlowMoles,
            float maxDifferentialKpa,
            float deltaTime,
            out float differentialKpa,
            out bool stalled)
        {
            float outletPressure = outlet.GetPressure(AtmosConstants.DefaultGasCount);
            differentialKpa = 0f;
            stalled = false;

            if (!ShouldPumpToOutlet(outletPressure, targetOutletPressureKpa))
                return 0f;

            float curveMoles = AtmosPortFlow.ComputePumpFlowMoles(
                inletPressureKpa,
                outletPressure,
                ratedMaxFlowMoles,
                maxDifferentialKpa,
                deltaTime,
                out differentialKpa,
                out stalled);

            return Mathf.Min(curveMoles, AtmosPortFlow.ComputeMolesToTarget(
                outletPressure,
                targetOutletPressureKpa,
                outlet.Temperature,
                outlet.Volume));
        }

        /// <summary>
        /// Moles a volume pump moves this tick: its set volume of inlet gas, up to what the pump
        /// curve allows, so it stalls against backpressure like any other pump.
        /// </summary>
        public static float ComputeVolumeMoles(
            GasPipeNetworkRecord inlet,
            float outletPressureKpa,
            int litresPerSecond,
            float ratedMaxFlowMoles,
            float maxDifferentialKpa,
            float deltaTime,
            out float differentialKpa,
            out bool stalled)
        {
            float curveMoles = AtmosPortFlow.ComputePumpFlowMoles(
                inlet.GetPressure(AtmosConstants.DefaultGasCount),
                outletPressureKpa,
                ratedMaxFlowMoles,
                maxDifferentialKpa,
                deltaTime,
                out differentialKpa,
                out stalled);

            return Mathf.Min(curveMoles, AtmosPortFlow.ComputeVolumeMoles(
                inlet.GetTotalMoles(AtmosConstants.DefaultGasCount),
                inlet.Volume,
                litresPerSecond,
                deltaTime));
        }

        [Server]
        public void ServerSetTargetOutletPressureKpa(int targetKpa)
        {
            _targetOutletPressureKpa = Mathf.Clamp(targetKpa, 0, 9000);
        }

        [Server]
        public void ServerSetTargetLitresPerSecond(int litresPerSecond)
        {
            _targetLitresPerSecond = Mathf.Clamp(litresPerSecond, 0, AtmosPortConstants.VolumePumpMaxLitresPerSecond);
        }

        protected override bool ShouldAnimateWhileIdle() => false;

        protected override bool RunInlineTick(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord inlet,
            GasPipeNetworkRecord outlet,
            float deltaTime)
        {
            float inletPressure = inlet.GetPressure(AtmosConstants.DefaultGasCount);
            float outletPressure = outlet.GetPressure(AtmosConstants.DefaultGasCount);
            float differentialKpa;
            bool stalled;

            float moles = _pumpRegulation == PumpRegulation.Volume
                ? ComputeVolumeMoles(
                    inlet,
                    outletPressure,
                    _targetLitresPerSecond,
                    RatedMaxFlowMolesPerSecond,
                    MaxDifferentialKpa,
                    deltaTime,
                    out differentialKpa,
                    out stalled)
                : ComputePressureMoles(
                    inletPressure,
                    outlet,
                    _targetOutletPressureKpa,
                    RatedMaxFlowMolesPerSecond,
                    MaxDifferentialKpa,
                    deltaTime,
                    out differentialKpa,
                    out stalled);

            LastDifferentialKpa = differentialKpa;
            LastStalled = stalled;
            return RecordFlow(registry.MoveGas(inlet, outlet, moles), inletPressure, outletPressure, deltaTime);
        }

        protected override void ApplyDeviceSpecificAnimatorState(bool flowing)
        {
            if (_animator == null)
                return;

            _animator.SetBool(PumpActiveId, flowing);
        }
    }
}
