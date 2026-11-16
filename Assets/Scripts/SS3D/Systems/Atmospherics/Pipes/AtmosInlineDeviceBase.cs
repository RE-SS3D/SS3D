using FishNet.Object.Synchronizing;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// A device in a gap in a Surface run, moving gas between the network behind it and the one in
    /// front. Nothing here changes topology, so running or flipping one never rebuilds a network.
    /// </summary>
    public abstract class AtmosInlineDeviceBase : AtmosPortControllerBase, IPipePortDevice
    {
        // Each cap closes the gap between a body shorter than its tile and the pipe at that port.
        [SerializeField]
        private GameObject _inletCap;

        [SerializeField]
        private GameObject _outletCap;

        [SyncVar(OnChange = nameof(HandlePortJoinedChanged))]
        private bool _inletJoined;

        [SyncVar(OnChange = nameof(HandlePortJoinedChanged))]
        private bool _outletJoined;

        private GasPipeNetworkId _inletId = GasPipeNetworkId.None;
        private GasPipeNetworkId _outletId = GasPipeNetworkId.None;
        private int _resolvedPortsVersion = -1;

        public override TileLayer[] PipeLayers => AtmosDevicePipeResolver.InlineLayers;

        public TileLayer[] PortLines => PipeLayers;

        public abstract PumpRegulation Regulation { get; }

        public bool InletJoined => _inletJoined;

        public bool OutletJoined => _outletJoined;

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyPortCaps();
        }

        public float LastFlowMolesPerSecond { get; private set; }

        public float LastDifferentialKpa { get; protected set; }

        public float LastInletPressureKpa { get; private set; }

        public float LastOutletPressureKpa { get; private set; }

        public bool LastStalled { get; protected set; }

        /// <summary>
        /// Connected means both ports reach a network; gas has nowhere to go otherwise. The network
        /// reported is the outlet, the side an inline device works on.
        /// </summary>
        public override bool TryGetConnectedNetwork(out GasPipeNetworkId networkId)
        {
            networkId = _outletId;
            return !_inletId.IsNone && !_outletId.IsNone;
        }

        public bool HasPortToward(Direction direction) =>
            TileObject != null && AtmosDevicePipeResolver.HasInlinePortToward(TileObject.Direction, direction);

        protected abstract bool RunInlineTick(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord inlet,
            GasPipeNetworkRecord outlet,
            float deltaTime);

        /// <summary>
        /// Stores what the device's gauge shows and says whether it counts as flowing.
        /// </summary>
        protected bool RecordFlow(float movedMoles, float inletPressureKpa, float outletPressureKpa, float deltaTime)
        {
            LastInletPressureKpa = inletPressureKpa;
            LastOutletPressureKpa = outletPressureKpa;
            LastFlowMolesPerSecond = deltaTime > 0f ? movedMoles / deltaTime : 0f;
            return movedMoles > AtmosPortConstants.FlowingMolesThreshold;
        }

        protected sealed override bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime)
        {
            GasPipeNetworkRegistry registry = pipeSimulation.Registry;
            return registry.TryGetNetwork(_inletId, out GasPipeNetworkRecord inlet)
                && registry.TryGetNetwork(_outletId, out GasPipeNetworkRecord outlet)
                && RunInlineTick(registry, inlet, outlet, deltaTime);
        }

        protected override void ResolveConnectedNetwork()
        {
            if (!TryGetPipeContext(out TileMap map, out GasPipeNetworkRegistry registry))
                return;

            int topologyVersion = registry.TopologyVersion;
            if (_resolvedPortsVersion == topologyVersion
                && registry.TryGetNetwork(_inletId, out _)
                && registry.TryGetNetwork(_outletId, out _))
            {
                return;
            }

            AtmosDevicePipeResolver.TryResolveInlineNetworks(
                map,
                registry,
                OriginTile,
                TileObject.Direction,
                out _inletId,
                out _outletId);
            _resolvedPortsVersion = topologyVersion;

            if (_inletJoined != !_inletId.IsNone)
                _inletJoined = !_inletId.IsNone;

            if (_outletJoined != !_outletId.IsNone)
                _outletJoined = !_outletId.IsNone;
        }

        private void HandlePortJoinedChanged(bool _, bool __, bool asServer)
        {
            if (!asServer)
                ApplyPortCaps();
        }

        private void ApplyPortCaps()
        {
            if (_inletCap != null)
                _inletCap.SetActive(_inletJoined);

            if (_outletCap != null)
                _outletCap.SetActive(_outletJoined);
        }
    }
}
