using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Releases a line's gas into the room through a floor nozzle (design §6). Unlike a vent it has no
    /// target pressure and no air alarm: turned on by hand, it pours until the pressures level.
    /// </summary>
    public sealed class AtmosInjectorController : AtmosPortControllerBase
    {
        [SerializeField]
        private Sprite _onIcon;

        [SerializeField]
        private Sprite _offIcon;

        public override TileLayer[] PipeLayers => AtmosDevicePipeResolver.AnyLineLayers;

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerSetEnabled(false);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new ToggleInteraction
                {
                    OnName = "Close injector",
                    OffName = "Open injector",
                    IconOn = _onIcon,
                    IconOff = _offIcon,
                },
            };
        }

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

            float budgetMoles = AtmosPortFlow.ComputeFlowMoles(
                network.GetPressure(AtmosConstants.DefaultGasCount),
                turfSimulation.GetCellPressure(OriginTile),
                AtmosPortConstants.InjectorRatedFlowMolesPerSecond,
                AtmosPortConstants.PortMaxDifferentialKpa,
                deltaTime);

            return ReleaseToTurf(pipeSimulation, networkId, network, budgetMoles);
        }
    }
}
