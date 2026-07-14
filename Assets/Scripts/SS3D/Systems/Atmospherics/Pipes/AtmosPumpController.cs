using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Turf→pipe pump with target outlet pressure (design §7). Not area-linked — operates on its tile and pipe network only.
    /// </summary>
    public sealed class AtmosPumpController : AtmosPortControllerBase
    {
        private static readonly int PumpActiveId = Animator.StringToHash("pumpActive");

        [SerializeField]
        private float _ratedMaxFlowMoles = AtmosPortConstants.PumpRatedFlowMolesPerSecond;

        [SerializeField]
        private float _maxDifferentialKpa = AtmosPortConstants.PumpMaxDifferentialKpa;

        [SyncVar]
        private int _targetOutletPressureKpa = 4500;

        private float _lastFlowMolesPerSecond;
        private float _lastDifferentialKpa;
        private float _lastInletPressureKpa;
        private float _lastOutletPressureKpa;
        private bool _lastStalled;

        public float RatedMaxFlowMolesPerSecond =>
            _ratedMaxFlowMoles > 0f ? _ratedMaxFlowMoles : AtmosPortConstants.PumpRatedFlowMolesPerSecond;

        public float MaxDifferentialKpa =>
            _maxDifferentialKpa > 0f ? _maxDifferentialKpa : AtmosPortConstants.PumpMaxDifferentialKpa;

        public int TargetOutletPressureKpa => _targetOutletPressureKpa;

        public float LastFlowMolesPerSecond => _lastFlowMolesPerSecond;

        public float LastDifferentialKpa => _lastDifferentialKpa;

        public float LastInletPressureKpa => _lastInletPressureKpa;

        public float LastOutletPressureKpa => _lastOutletPressureKpa;

        public bool LastStalled => _lastStalled;

        public static bool ShouldPumpToOutlet(float outletPressureKpa, int targetOutletPressureKpa) =>
            targetOutletPressureKpa > 0 && outletPressureKpa < targetOutletPressureKpa;

        [Server]
        public void ServerSetTargetOutletPressureKpa(int targetKpa)
        {
            _targetOutletPressureKpa = Mathf.Clamp(targetKpa, 0, 9000);
        }

        protected override bool ShouldAnimateWhileIdle() => false;

        protected override bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime)
        {
            _lastFlowMolesPerSecond = 0f;
            _lastDifferentialKpa = 0f;
            _lastInletPressureKpa = 0f;
            _lastOutletPressureKpa = 0f;
            _lastStalled = false;

            if (!TryGetConnectedNetwork(out GasPipeNetworkId networkId)
                || !pipeSimulation.Registry.TryGetNetwork(networkId, out GasPipeNetworkRecord network))
            {
                return false;
            }

            TileCoord turfCell = OriginTile;
            float inletPressure = turfSimulation.GetCellPressure(turfCell);
            float outletPressure = network.GetPressure(AtmosConstants.DefaultGasCount);
            _lastInletPressureKpa = inletPressure;
            _lastOutletPressureKpa = outletPressure;

            if (!ShouldPumpToOutlet(outletPressure, _targetOutletPressureKpa))
                return false;

            float budgetMoles = AtmosPortFlow.ComputePumpFlowMoles(
                inletPressure,
                outletPressure,
                RatedMaxFlowMolesPerSecond,
                MaxDifferentialKpa,
                deltaTime,
                out float differentialKpa,
                out bool stalled);

            _lastDifferentialKpa = differentialKpa;
            _lastStalled = stalled;
            _lastFlowMolesPerSecond = deltaTime > 0f ? budgetMoles / deltaTime : 0f;

            if (budgetMoles <= 0f)
                return false;

            return PullTurfToNetwork(pipeSimulation, turfSimulation, networkId, turfCell, budgetMoles);
        }

        protected override void ApplyDeviceSpecificAnimatorState(bool flowing)
        {
            if (_animator == null)
                return;

            _animator.SetBool(PumpActiveId, flowing);
        }

        private static bool PullTurfToNetwork(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            GasPipeNetworkId networkId,
            TileCoord turfCell,
            float budgetMoles)
        {
            float totalMoles = 0f;
            for (int gasId = 0; gasId < AtmosConstants.DefaultGasCount; gasId++)
            {
                if (turfSimulation.TryGetGasMoles(turfCell, new GasId((ushort)gasId), out float available))
                    totalMoles += available;
            }

            if (totalMoles <= 0f)
                return false;

            budgetMoles = Mathf.Min(budgetMoles, totalMoles);
            bool movedAny = false;
            float remainingBudget = budgetMoles;

            for (int gasId = 0; gasId < AtmosConstants.DefaultGasCount; gasId++)
            {
                if (remainingBudget <= 0f)
                    break;

                if (!turfSimulation.TryGetGasMoles(turfCell, new GasId((ushort)gasId), out float available) || available <= 0f)
                    continue;

                float share = available / totalMoles;
                float request = Mathf.Min(remainingBudget, budgetMoles * share);
                if (pipeSimulation.TryTransferMoles(
                        networkId,
                        new GasId((ushort)gasId),
                        request,
                        turfCell,
                        PipeTransferDirection.ToNetwork,
                        out float moved)
                    && moved > 0f)
                {
                    remainingBudget -= moved;
                    movedAny = true;
                }
            }

            return movedAny;
        }
    }
}
