using SS3D.Systems.Atmospherics.Pipes;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Visual adjacency for atmos pipes — same generic/specific type on the pipe layer, or a device
    /// port facing the pipe. Shared surface for gas pipe network topology (#1464).
    /// </summary>
    public sealed class PipeConnectionRule : ICrossLayerConnectionRule
    {
        private readonly SimpleConnectionRule _inner;

        public PipeConnectionRule(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            _inner = new SimpleConnectionRule(genericType, specificType);
        }

        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour) =>
            _inner.IsConnected(self, neighbour) || IsDevicePortFacing(self, neighbour);

        /// <summary>
        /// A manifold sits on its own layer and a floor plate on Surface, so a pipe's same-layer
        /// neighbours never show them.
        /// </summary>
        public bool IsConnectedAcrossLayers(PlacedTileObject self, Direction direction, TileMap map)
        {
            if (!TileHelper.IsCardinal(direction))
                return false;

            TileCoord neighbour = AtmosPipeConnectivity.Offset(new TileCoord(self.MapId, self.WorldOrigin), direction);
            Direction backToPipe = TileHelper.GetOpposite(direction);
            return AtmosPipeConnectivity.TryGetManifoldPort(map, neighbour, self.Layer, backToPipe, out _)
                || AtmosPipeConnectivity.HasDevicePort(map, neighbour, self.Layer, backToPipe);
        }

        public static bool ParticipatesInGasNetwork(PlacedTileObject segment) =>
            segment != null
            && ((segment.GenericType == TileObjectGenericType.Pipe
                    && segment.Connector is EngineDrivenHorizontalConnector
                    && GetPipeMedium(segment) == PipeMedium.Gas)
                || segment.TryGetComponent<GasPipeManifold>(out _));

        /// <summary>
        /// A pipe draws a run into a device's port, but gas doesn't: the device keeps the networks
        /// on its two sides apart, which <see cref="IsPipeSegmentConnected"/> holds to.
        /// </summary>
        private static bool IsDevicePortFacing(PlacedTileObject self, PlacedTileObject neighbour) =>
            neighbour != null
            && neighbour.TryGetComponent(out IPipePortDevice device)
            && neighbour.NeighbourAtDirectionOf(self, out Direction towardPipe)
            && device.HasPortToward(towardPipe);

        public static PipeMedium GetPipeMedium(PlacedTileObject segment)
        {
            // All current atmos pipe assets are gas-rated. Liquid pipes will use a distinct type later.
            return PipeMedium.Gas;
        }

        public static bool IsPipeSegmentConnected(PlacedTileObject self, PlacedTileObject neighbour)
        {
            if (!ParticipatesInGasNetwork(self) || !ParticipatesInGasNetwork(neighbour))
                return false;

            if (self.Layer != neighbour.Layer)
                return false;

            if (self.Connector is not IEngineDrivenAdjacency engineDriven)
                return false;

            IConnectionRule selfRule = engineDriven.ConnectionRule;
            return selfRule != null && selfRule.IsConnected(self, neighbour);
        }
    }
}
