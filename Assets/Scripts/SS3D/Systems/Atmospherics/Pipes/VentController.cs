using FishNet.Object.Synchronizing;
using FishNet.Object;
using System;
using SS3D.Core;
using SS3D.Systems.Audio;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pushes connected pipe network gas into the turf cell at the device coordinate.
    /// </summary>
    public sealed class VentController : AtmosPortControllerBase, IAmbientLoopState
    {
        private static readonly int VentActiveId = Animator.StringToHash("ventActive");

        private const int DefaultTargetPressureKpa = 101;

        [SyncVar]
        private int _targetPressureKpa = DefaultTargetPressureKpa;

        public int TargetPressureKpa => _targetPressureKpa;

        public static bool ShouldVentToTurf(float turfPressureKpa, int targetPressureKpa) =>
            targetPressureKpa > 0 && turfPressureKpa < targetPressureKpa;

        [Server]
        public void ServerSetTargetPressureKpa(int targetKpa)
        {
            _targetPressureKpa = Mathf.Clamp(targetKpa, 0, 200);
        }

        [Serializable]
        private sealed class VentSetup : PortSetup
        {
            public int targetPressureKpa = DefaultTargetPressureKpa;
        }

        protected override PortSetup DefaultSetup() => new VentSetup();

        protected override PortSetup CurrentSetup() => new VentSetup { enabled = IsEnabled, targetPressureKpa = _targetPressureKpa };

        protected override void ApplySetup(PortSetup setup)
        {
            base.ApplySetup(setup);
            if (setup is VentSetup vent)
                ServerSetTargetPressureKpa(vent.targetPressureKpa);
        }

        /// <summary>Pressure in the room and in the line this vent draws on; the line reads 0 with no pipe.</summary>
        public void GetPressures(out float roomKpa, out float lineKpa)
        {
            roomKpa = 0f;
            lineKpa = 0f;
            if (!SubSystems.TryGet(out AtmosSubSystem atmos) || atmos.Simulation == null)
                return;

            roomKpa = atmos.Simulation.GetCellPressure(OriginTile);
            if (TryGetConnectedNetwork(out GasPipeNetworkId networkId)
                && atmos.PipeRegistry.TryGetNetwork(networkId, out GasPipeNetworkRecord network))
            {
                lineKpa = network.GetPressure(AtmosConstants.DefaultGasCount);
            }
        }

        public override TileLayer[] PipeLayers => AtmosDevicePipeResolver.VentLayers;

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
            if (!ShouldVentToTurf(turfPressure, _targetPressureKpa))
            {
                return false;
            }

            float budgetMoles = AtmosPortFlow.ComputeFlowMoles(
                networkPressure,
                turfPressure,
                AtmosPortConstants.VentRatedFlowMolesPerSecond,
                AtmosPortConstants.VentFullFlowDifferentialKpa,
                deltaTime);

            return ReleaseToTurf(pipeSimulation, networkId, network, budgetMoles);
        }

        protected override void ApplyDeviceSpecificAnimatorState(bool flowing)
        {
            if (_animator == null)
                return;

            _animator.SetBool(VentActiveId, flowing);
        }
    }
}
