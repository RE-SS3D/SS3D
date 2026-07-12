using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pushes connected pipe network gas into the turf cell at the device coordinate.
    /// </summary>
    public sealed class VentController : AtmosPortControllerBase
    {
        private static readonly int VentActiveId = Animator.StringToHash("ventActive");

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
            float networkPressure = network.GetPressure(AtmosConstants.DefaultGasCount);
            float turfPressure = turfSimulation.GetCellPressure(turfCell);
            float budgetMoles = AtmosPortFlow.ComputeFlowMoles(
                networkPressure,
                turfPressure,
                AtmosPortConstants.VentRatedFlowMolesPerSecond,
                AtmosPortConstants.PortMaxDifferentialKpa,
                deltaTime);

            if (budgetMoles <= 0f)
                return false;

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

        protected override void ApplyDeviceSpecificAnimatorState(bool active)
        {
            if (_animator == null)
                return;

            _animator.SetBool(VentActiveId, active);
        }
    }
}
