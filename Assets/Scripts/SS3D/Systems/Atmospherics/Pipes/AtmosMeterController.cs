using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Reads the line on its tile (design §6) and keeps a second-by-second pressure history for its
    /// panel. Moves no gas and needs no power.
    /// </summary>
    public sealed class AtmosMeterController : AtmosPortControllerBase
    {
        /// <summary>Seconds of pressure history a meter keeps.</summary>
        public const int HistoryLength = 40;

        private const float SampleSeconds = 1f;

        private readonly float[] _history = new float[HistoryLength];
        private int _newest = HistoryLength - 1;
        private float _sinceSample = SampleSeconds;

        public override TileLayer[] PipeLayers => AtmosDevicePipeResolver.AnyLineLayers;

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
    }
}
