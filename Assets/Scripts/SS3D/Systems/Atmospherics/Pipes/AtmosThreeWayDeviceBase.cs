using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// A device with a straight run and a side port, each on its own network. A filter sends gas out
    /// of the side; a mixer takes it in there. Like an inline device, running one never rebuilds a
    /// network. A machine form works on the Surface line; a floor plate works on the underfloor line
    /// its ports reach.
    /// </summary>
    public abstract class AtmosThreeWayDeviceBase : AtmosPortControllerBase, IPipePortDevice
    {
        [SerializeField]
        private TileLayer[] _lines = AtmosDevicePipeResolver.InlineLayers;

        // Left or right of the way the device faces, matching the L or R mesh.
        [SerializeField]
        private bool _sideOnLeft = true;

        private GasPipeNetworkId _backId = GasPipeNetworkId.None;
        private GasPipeNetworkId _frontId = GasPipeNetworkId.None;
        private GasPipeNetworkId _sideId = GasPipeNetworkId.None;
        private int _resolvedPortsVersion = -1;

        public override TileLayer[] PipeLayers => _lines;

        public TileLayer[] PortLines => _lines;

        public bool SideOnLeft => _sideOnLeft;

        public float LastFlowMolesPerSecond { get; private set; }

        public bool LastStalled { get; protected set; }

        /// <summary>
        /// Connected means all three ports reach a network. The network reported is the front.
        /// </summary>
        public override bool TryGetConnectedNetwork(out GasPipeNetworkId networkId)
        {
            networkId = _frontId;
            return !_backId.IsNone && !_frontId.IsNone && !_sideId.IsNone;
        }

        public bool HasPortToward(Direction direction) =>
            TileObject != null && AtmosDevicePipeResolver.HasThreeWayPortToward(TileObject.Direction, _sideOnLeft, direction);

        /// <summary>
        /// The live networks at the ports, for panels. Read within the tick that asks.
        /// </summary>
        public bool TryGetPorts(out GasPipeNetworkRecord back, out GasPipeNetworkRecord front, out GasPipeNetworkRecord side)
        {
            back = null;
            front = null;
            side = null;
            return TryGetPipeContext(out _, out GasPipeNetworkRegistry registry)
                && registry.TryGetNetwork(_backId, out back)
                && registry.TryGetNetwork(_frontId, out front)
                && registry.TryGetNetwork(_sideId, out side);
        }

        protected abstract bool RunThreeWayTick(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord back,
            GasPipeNetworkRecord front,
            GasPipeNetworkRecord side,
            float deltaTime);

        /// <summary>
        /// Stores what the panel shows and says whether the device counts as flowing.
        /// </summary>
        protected bool RecordFlow(float movedMoles, float deltaTime)
        {
            LastFlowMolesPerSecond = deltaTime > 0f ? movedMoles / deltaTime : 0f;
            return movedMoles > AtmosPortConstants.FlowingMolesThreshold;
        }

        protected sealed override bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime)
        {
            GasPipeNetworkRegistry registry = pipeSimulation.Registry;
            return registry.TryGetNetwork(_backId, out GasPipeNetworkRecord back)
                && registry.TryGetNetwork(_frontId, out GasPipeNetworkRecord front)
                && registry.TryGetNetwork(_sideId, out GasPipeNetworkRecord side)
                && RunThreeWayTick(registry, back, front, side, deltaTime);
        }

        protected override void ResolveConnectedNetwork()
        {
            if (!TryGetPipeContext(out TileMap map, out GasPipeNetworkRegistry registry))
                return;

            int topologyVersion = registry.TopologyVersion;
            if (_resolvedPortsVersion == topologyVersion
                && registry.TryGetNetwork(_backId, out _)
                && registry.TryGetNetwork(_frontId, out _)
                && registry.TryGetNetwork(_sideId, out _))
            {
                return;
            }

            AtmosDevicePipeResolver.TryResolveThreeWayNetworks(
                map,
                registry,
                OriginTile,
                TileObject.Direction,
                _sideOnLeft,
                _lines,
                out _backId,
                out _frontId,
                out _sideId);
            _resolvedPortsVersion = topologyVersion;
        }
    }
}
