using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Resolves the gas pipe networks a device attaches to: the first of its lines present on its own
    /// tile, or for an inline device the pipes at its two ports.
    /// </summary>
    public static class AtmosDevicePipeResolver
    {
        /// <summary>Supply, then Mixed.</summary>
        public static readonly TileLayer[] VentLayers = { TileLayer.PipeLeft, TileLayer.PipeMiddle };

        /// <summary>Waste, then Mixed.</summary>
        public static readonly TileLayer[] ScrubberLayers = { TileLayer.PipeRight, TileLayer.PipeMiddle };

        /// <summary>Surface only; nothing attaches to it automatically but inline devices, at their ports.</summary>
        public static readonly TileLayer[] InlineLayers = { TileLayer.PipeSurface };

        /// <summary>The first of these on a tile is the line a meter reads or an injector draws from.</summary>
        public static readonly TileLayer[] AnyLineLayers =
        {
            TileLayer.PipeSurface,
            TileLayer.PipeLeft,
            TileLayer.PipeMiddle,
            TileLayer.PipeRight,
        };

        /// <summary>The lines a floor plate can sit on, in the plate's own gap.</summary>
        public static readonly TileLayer[] PlateLayers =
        {
            TileLayer.PipeLeft,
            TileLayer.PipeMiddle,
            TileLayer.PipeRight,
        };

        /// <summary>Plate lines plus Surface, for a plate that doubles as a Surface device.</summary>
        public static readonly TileLayer[] PlateOrSurfaceLayers =
        {
            TileLayer.PipeLeft,
            TileLayer.PipeMiddle,
            TileLayer.PipeRight,
            TileLayer.PipeSurface,
        };

        /// <summary>
        /// Gas leaves an inline device through its front and enters through its back, so its facing
        /// decides which way gas moves.
        /// </summary>
        public static bool HasInlinePortToward(Direction facing, Direction direction) =>
            direction == facing || direction == TileHelper.GetOpposite(facing);

        /// <summary>
        /// The side port of a three-way device: its left or right, looking the way it faces.
        /// </summary>
        public static Direction GetSidePort(Direction facing, bool sideOnLeft) =>
            sideOnLeft ? TileHelper.GetPreviousCardinalDir(facing) : TileHelper.GetNextCardinalDir(facing);

        public static bool HasThreeWayPortToward(Direction facing, bool sideOnLeft, Direction direction) =>
            HasInlinePortToward(facing, direction) || direction == GetSidePort(facing, sideOnLeft);

        /// <summary>
        /// The line a three-way device works on: the one of <paramref name="lines"/> with no pipe on
        /// the device's tile and something at all three ports. None, or more than one, means it
        /// stays disconnected; Map Check says so through <see cref="HasThreeWayPipes"/>.
        /// </summary>
        public static bool TryResolveThreeWayLine(
            TileMap map,
            TileCoord deviceCoord,
            Direction facing,
            bool sideOnLeft,
            TileLayer[] lines,
            out TileLayer line)
        {
            line = default;
            if (map == null)
                return false;

            Direction front = facing;
            Direction back = TileHelper.GetOpposite(facing);
            Direction side = GetSidePort(facing, sideOnLeft);
            int matches = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                TileLayer candidate = lines[i];
                if (AtmosPipeConnectivity.TryGetSegment(map, deviceCoord, candidate, out _)
                    || !HasPortLine(map, deviceCoord, front, candidate)
                    || !HasPortLine(map, deviceCoord, back, candidate)
                    || !HasPortLine(map, deviceCoord, side, candidate))
                {
                    continue;
                }

                line = candidate;
                matches++;
            }

            return matches == 1;
        }

        /// <summary>
        /// The networks at a three-way device's ports, on its one line.
        /// </summary>
        public static bool TryResolveThreeWayNetworks(
            TileMap map,
            GasPipeNetworkRegistry registry,
            TileCoord deviceCoord,
            Direction facing,
            bool sideOnLeft,
            TileLayer[] lines,
            out GasPipeNetworkId backId,
            out GasPipeNetworkId frontId,
            out GasPipeNetworkId sideId)
        {
            backId = GasPipeNetworkId.None;
            frontId = GasPipeNetworkId.None;
            sideId = GasPipeNetworkId.None;

            if (!TryResolveThreeWayLine(map, deviceCoord, facing, sideOnLeft, lines, out TileLayer line))
                return false;

            return TryResolvePort(map, registry, deviceCoord, TileHelper.GetOpposite(facing), line, out backId)
                & TryResolvePort(map, registry, deviceCoord, facing, line, out frontId)
                & TryResolvePort(map, registry, deviceCoord, GetSidePort(facing, sideOnLeft), line, out sideId);
        }

        /// <summary>
        /// Whether a three-way device finds its line. Reads the tilemap, like <see cref="HasPipeSegment"/>.
        /// </summary>
        public static bool HasThreeWayPipes(TileMap map, TileCoord deviceCoord, Direction facing, bool sideOnLeft, TileLayer[] lines) =>
            TryResolveThreeWayLine(map, deviceCoord, facing, sideOnLeft, lines, out _);

        /// <summary>
        /// The networks at an inline device's two ports: a Surface pipe, or a manifold port facing
        /// the device. Either is None when nothing is there, and the result says whether both were.
        /// </summary>
        public static bool TryResolveInlineNetworks(
            TileMap map,
            GasPipeNetworkRegistry registry,
            TileCoord deviceCoord,
            Direction facing,
            out GasPipeNetworkId inletId,
            out GasPipeNetworkId outletId)
        {
            bool hasInlet = TryResolvePort(map, registry, deviceCoord, TileHelper.GetOpposite(facing), TileLayer.PipeSurface, out inletId);
            bool hasOutlet = TryResolvePort(map, registry, deviceCoord, facing, TileLayer.PipeSurface, out outletId);
            return hasInlet && hasOutlet;
        }

        /// <summary>
        /// Whether both of an inline device's ports reach a line. Reads the tilemap, like
        /// <see cref="HasPipeSegment"/>.
        /// </summary>
        public static bool HasInlinePipes(TileMap map, TileCoord deviceCoord, Direction facing) =>
            HasPortLine(map, deviceCoord, facing, TileLayer.PipeSurface)
            && HasPortLine(map, deviceCoord, TileHelper.GetOpposite(facing), TileLayer.PipeSurface);

        private static bool TryResolvePort(
            TileMap map,
            GasPipeNetworkRegistry registry,
            TileCoord deviceCoord,
            Direction port,
            TileLayer line,
            out GasPipeNetworkId networkId)
        {
            TileCoord portCoord = AtmosPipeConnectivity.Offset(deviceCoord, port);
            if (AtmosPipeConnectivity.TryGetSegment(map, portCoord, line, out PlacedTileObject segment)
                && registry.TryGetNetworkForSegment(GasPipeSegmentKey.From(segment), out networkId, out _))
            {
                return true;
            }

            networkId = GasPipeNetworkId.None;
            return map != null
                && AtmosPipeConnectivity.TryGetManifoldPort(map, portCoord, line, TileHelper.GetOpposite(port), out GasPipeManifold manifold)
                && registry.TryGetNetworkForSegment(GasPipeSegmentKey.From(manifold.PlacedObject), out networkId, out _);
        }

        private static bool HasPortLine(TileMap map, TileCoord deviceCoord, Direction port, TileLayer line)
        {
            TileCoord portCoord = AtmosPipeConnectivity.Offset(deviceCoord, port);
            return AtmosPipeConnectivity.TryGetSegment(map, portCoord, line, out _)
                || AtmosPipeConnectivity.TryGetManifoldPort(map, portCoord, line, TileHelper.GetOpposite(port), out _);
        }

        public static bool TryResolveNetwork(
            TileMap map,
            GasPipeNetworkRegistry registry,
            TileCoord deviceCoord,
            TileLayer[] layers,
            out GasPipeNetworkId networkId,
            out GasPipeSegmentKey segmentKey)
        {
            networkId = GasPipeNetworkId.None;
            segmentKey = default;

            if (map == null || registry == null)
                return false;

            for (int i = 0; i < layers.Length; i++)
            {
                if (!AtmosPipeConnectivity.TryGetSegment(map, deviceCoord, layers[i], out PlacedTileObject segment))
                    continue;

                segmentKey = GasPipeSegmentKey.From(segment);
                if (registry.TryGetNetworkForSegment(segmentKey, out networkId, out _))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether a pipe sits where <see cref="TryResolveNetwork"/> looks for one. Reads the
        /// tilemap, so it needs no running atmos simulation.
        /// </summary>
        public static bool HasPipeSegment(TileMap map, TileCoord deviceCoord, TileLayer[] layers)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (AtmosPipeConnectivity.TryGetSegment(map, deviceCoord, layers[i], out _))
                    return true;
            }

            return false;
        }
    }
}
