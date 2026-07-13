using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Bidirectional pipe↔turf pump (design §7). Pair with <c>GasPumpGaugeController</c> for the diegetic gauge UI.
    /// </summary>
    public sealed class AtmosPumpController : AtmosPortControllerBase
    {
        private static readonly int PumpActiveId = Animator.StringToHash("pumpActive");

        [SerializeField]
        private float _ratedMaxFlowMoles = AtmosPortConstants.PumpRatedFlowMolesPerSecond;

        [SerializeField]
        private float _maxDifferentialKpa = AtmosPortConstants.PumpMaxDifferentialKpa;

        private float _lastFlowMolesPerSecond;
        private float _lastDifferentialKpa;
        private bool _lastStalled;

        public float RatedMaxFlowMolesPerSecond =>
            _ratedMaxFlowMoles > 0f ? _ratedMaxFlowMoles : AtmosPortConstants.PumpRatedFlowMolesPerSecond;

        public float MaxDifferentialKpa =>
            _maxDifferentialKpa > 0f ? _maxDifferentialKpa : AtmosPortConstants.PumpMaxDifferentialKpa;

        public float LastFlowMolesPerSecond => _lastFlowMolesPerSecond;

        public float LastDifferentialKpa => _lastDifferentialKpa;

        public bool LastStalled => _lastStalled;

        protected override bool ShouldAnimateWhileIdle() => false;

        protected override bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime)
        {
            _lastFlowMolesPerSecond = 0f;
            _lastDifferentialKpa = 0f;
            _lastStalled = false;

            if (!TryGetConnectedNetwork(out GasPipeNetworkId networkId)
                || !pipeSimulation.Registry.TryGetNetwork(networkId, out GasPipeNetworkRecord network))
            {
                return false;
            }

            TileCoord turfCell = OriginTile;
            float networkPressure = network.GetPressure(AtmosConstants.DefaultGasCount);
            float turfPressure = turfSimulation.GetCellPressure(turfCell);
            bool networkToTurf = networkPressure >= turfPressure;
            float sourcePressure = networkToTurf ? networkPressure : turfPressure;
            float destinationPressure = networkToTurf ? turfPressure : networkPressure;

            float budgetMoles = AtmosPortFlow.ComputePumpFlowMoles(
                sourcePressure,
                destinationPressure,
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

            if (networkToTurf)
                return PushNetworkToTurf(pipeSimulation, network, networkId, turfCell, budgetMoles);

            return PullTurfToNetwork(pipeSimulation, turfSimulation, networkId, turfCell, budgetMoles);
        }

        protected override void ApplyDeviceSpecificAnimatorState(bool flowing)
        {
            if (_animator == null)
                return;

            _animator.SetBool(PumpActiveId, flowing);
        }

        private static bool PushNetworkToTurf(
            AtmosPipeSimulation pipeSimulation,
            GasPipeNetworkRecord network,
            GasPipeNetworkId networkId,
            TileCoord turfCell,
            float budgetMoles)
        {
            float totalMoles = network.GetTotalMoles(AtmosConstants.DefaultGasCount);
            if (totalMoles <= 0f)
                return false;

            budgetMoles = Mathf.Min(budgetMoles, totalMoles);
            bool movedAny = false;

            for (int gasId = 0; gasId < AtmosConstants.DefaultGasCount; gasId++)
            {
                float gasMoles = network.Moles[gasId];
                if (gasMoles <= 0f)
                    continue;

                float share = gasMoles / totalMoles;
                float request = budgetMoles * share;
                if (pipeSimulation.TryTransferMoles(
                        networkId,
                        new GasId((ushort)gasId),
                        request,
                        turfCell,
                        PipeTransferDirection.ToTurf,
                        out float moved)
                    && moved > 0f)
                {
                    movedAny = true;
                }
            }

            return movedAny;
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
