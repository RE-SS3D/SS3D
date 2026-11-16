using System;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Reads one line on its tile (design §6) and keeps a second-by-second pressure history for its
    /// panel. Moves no gas and needs no power. With several lines on its tile it reads the one chosen
    /// in its panel, saved with the map; until then, or once that line is gone, the first it finds.
    /// </summary>
    public sealed class AtmosMeterController : AtmosPortControllerBase
    {
        /// <summary>Seconds of pressure history a meter keeps.</summary>
        public const int HistoryLength = 40;

        private const float SampleSeconds = 1f;

        private readonly float[] _history = new float[HistoryLength];
        private int _newest = HistoryLength - 1;
        private float _sinceSample = SampleSeconds;

        // The chosen line first, then the rest in AnyLineLayers order.
        private readonly TileLayer[] _layers = (TileLayer[])AtmosDevicePipeResolver.AnyLineLayers.Clone();
        private TileLayer? _chosenLine;

        public override TileLayer[] PipeLayers => _layers;

        /// <summary>The line picked in the panel, or null when the meter reads the first it finds.</summary>
        public TileLayer? ChosenLine => _chosenLine;

        [Serializable]
        private sealed class MeterSetup : PortSetup
        {
            // A TileLayer name; empty reads the first line found.
            public string line = string.Empty;
        }

        protected override PortSetup DefaultSetup() => new MeterSetup();

        protected override PortSetup CurrentSetup() =>
            new MeterSetup { enabled = IsEnabled, line = _chosenLine?.ToString() ?? string.Empty };

        protected override void ApplySetup(PortSetup setup)
        {
            base.ApplySetup(setup);
            if (setup is not MeterSetup meter)
                return;

            TileLayer? line = Enum.TryParse(meter.line, out TileLayer parsed)
                && Array.IndexOf(AtmosDevicePipeResolver.AnyLineLayers, parsed) >= 0
                    ? parsed
                    : null;
            SetChosenLine(line);
        }

        /// <summary>
        /// How many lines run through the meter's tile. Reads the tilemap.
        /// </summary>
        public int CountLinesOnTile()
        {
            if (!TryGetPipeContext(out TileMap map, out _))
                return 0;

            int count = 0;
            foreach (TileLayer layer in AtmosDevicePipeResolver.AnyLineLayers)
            {
                if (AtmosPipeConnectivity.TryGetSegment(map, OriginTile, layer, out _))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Server: reads the next line on the tile after the one it reads now, wrapping around.
        /// Does nothing with fewer than two lines there.
        /// </summary>
        public void ServerSelectNextLine()
        {
            if (!TryGetPipeContext(out TileMap map, out _))
                return;

            TileLayer[] order = AtmosDevicePipeResolver.AnyLineLayers;
            TileLayer? current = null;
            foreach (TileLayer layer in _layers)
            {
                if (AtmosPipeConnectivity.TryGetSegment(map, OriginTile, layer, out _))
                {
                    current = layer;
                    break;
                }
            }

            if (current == null)
                return;

            int start = Array.IndexOf(order, current.Value);
            for (int step = 1; step < order.Length; step++)
            {
                TileLayer candidate = order[(start + step) % order.Length];
                if (!AtmosPipeConnectivity.TryGetSegment(map, OriginTile, candidate, out _))
                    continue;

                SetChosenLine(candidate);
                ResolveConnectedNetwork();
                return;
            }
        }

        /// <summary>
        /// The live network under the meter, for its panel. Read within the tick that asks.
        /// </summary>
        public bool TryGetLine(out GasPipeNetworkRecord line)
        {
            line = null;
            return TryGetConnectedNetwork(out GasPipeNetworkId networkId)
                && TryGetPipeContext(out _, out GasPipeNetworkRegistry registry)
                && registry.TryGetNetwork(networkId, out line);
        }

        /// <summary>
        /// Copies the history into <paramref name="target"/>, oldest first.
        /// </summary>
        public void CopyHistory(float[] target)
        {
            for (int i = 0; i < HistoryLength; i++)
                target[i] = _history[(_newest + 1 + i) % HistoryLength];
        }

        public void Sample(float pressureKpa, float deltaTime)
        {
            _sinceSample += deltaTime;
            if (_sinceSample < SampleSeconds)
                return;

            _sinceSample = 0f;
            _newest = (_newest + 1) % HistoryLength;
            _history[_newest] = pressureKpa;
        }

        protected override bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime)
        {
            if (TryGetConnectedNetwork(out GasPipeNetworkId networkId)
                && pipeSimulation.Registry.TryGetNetwork(networkId, out GasPipeNetworkRecord line))
            {
                Sample(line.GetPressure(AtmosConstants.DefaultGasCount), deltaTime);
            }

            return false;
        }

        private void SetChosenLine(TileLayer? line)
        {
            if (_chosenLine == line)
                return;

            _chosenLine = line;
            TileLayer[] order = AtmosDevicePipeResolver.AnyLineLayers;
            int next = 0;
            if (line.HasValue)
                _layers[next++] = line.Value;

            foreach (TileLayer layer in order)
            {
                if (layer != line)
                    _layers[next++] = layer;
            }

            // A new line's history starts fresh; the next tick samples it.
            Array.Clear(_history, 0, HistoryLength);
            _sinceSample = SampleSeconds;
            InvalidateConnectedNetwork();
        }
    }
}
