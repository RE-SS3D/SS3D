using SS3D.Systems.Atmospherics.Pipes;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Visual adjacency for atmos pipes — same generic/specific type on the pipe layer.
    /// Shared surface for gas pipe network topology (#1464).
    /// </summary>
    public sealed class PipeConnectionRule : IConnectionRule
    {
        private readonly SimpleConnectionRule _inner;

        public PipeConnectionRule(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            _inner = new SimpleConnectionRule(genericType, specificType);
        }

        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour) =>
            _inner.IsConnected(self, neighbour);

        public static bool ParticipatesInGasNetwork(PlacedTileObject segment) =>
            segment != null
            && segment.GenericType == TileObjectGenericType.Pipe
            && segment.Connector is EngineDrivenHorizontalConnector
            && GetPipeMedium(segment) == PipeMedium.Gas;

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
