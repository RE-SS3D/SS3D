using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pulls filtered gases from the turf cell into the connected pipe network.
    /// </summary>
    public sealed class ScrubberController : AtmosPortControllerBase
    {
        private static readonly GasId[] FilteredGases = { AtmosConstants.CarbonDioxide };
        private static readonly int ScrubActiveId = Animator.StringToHash("scrubActive");

        protected override bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime)
        {
            if (!TryGetConnectedNetwork(out GasPipeNetworkId networkId)
                || !pipeSimulation.Registry.TryGetNetwork(networkId, out GasPipeNetworkRecord network))
            {
                return false;
            }

            TileCoord turfCell = OriginTile;
            float turfPressure = turfSimulation.GetCellPressure(turfCell);
            float networkPressure = network.GetPressure(AtmosConstants.DefaultGasCount);
            float budgetMoles = AtmosPortFlow.ComputeFlowMoles(
                turfPressure,
                networkPressure,
                AtmosPortConstants.ScrubberRatedFlowMolesPerSecond,
                AtmosPortConstants.PortMaxDifferentialKpa,
                deltaTime);

            if (budgetMoles <= 0f)
                return false;

            bool movedAny = false;
            float remainingBudget = budgetMoles;

            foreach (GasId gasId in FilteredGases)
            {
                if (remainingBudget <= 0f)
                    break;

                if (!turfSimulation.TryGetGasMoles(turfCell, gasId, out float available) || available <= 0f)
                    continue;

                float request = Mathf.Min(remainingBudget, available);
                if (pipeSimulation.TryTransferMoles(
                        networkId,
                        gasId,
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

        protected override void ApplyDeviceSpecificAnimatorState(bool active)
        {
            if (_animator == null)
                return;

            _animator.SetBool(ScrubActiveId, active);
        }
    }
}
