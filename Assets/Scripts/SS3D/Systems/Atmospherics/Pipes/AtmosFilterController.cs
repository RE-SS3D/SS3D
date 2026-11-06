using FishNet.Object;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Splits a stream (design §6): selected gases leave by the side port, the rest carry straight
    /// on. If either outlet is full it stops.
    /// </summary>
    public sealed class AtmosFilterController : AtmosThreeWayDeviceBase
    {
        private const ulong AllGases = ulong.MaxValue;

        private int _flowLitresPerSecond = AtmosPortConstants.FilterDefaultLitresPerSecond;

        // Bit n selects gas id n, so a tick reads it without allocating.
        private ulong _selectedGases;

        public int FlowLitresPerSecond => _flowLitresPerSecond;

        public ulong SelectedGases => _selectedGases;

        public bool IsSelected(GasId gasId) => (_selectedGases & GasBit(gasId)) != 0UL;

        /// <summary>
        /// Whether either outlet has reached <see cref="AtmosPortConstants.FilterOutletLimitKpa"/>.
        /// </summary>
        public static bool IsStalled(float outletPressureKpa, float sidePressureKpa) =>
            outletPressureKpa >= AtmosPortConstants.FilterOutletLimitKpa
            || sidePressureKpa >= AtmosPortConstants.FilterOutletLimitKpa;

        /// <summary>
        /// Share of the inlet's mix to move this tick: the set flow rate, up to the rated moles.
        /// </summary>
        public static float ComputeFraction(GasPipeNetworkRecord inlet, int litresPerSecond, float deltaTime)
        {
            float inletMoles = inlet.GetTotalMoles(AtmosConstants.DefaultGasCount);
            if (inletMoles <= 0f)
                return 0f;

            float drawn = AtmosPortFlow.ComputeVolumeMoles(inletMoles, inlet.Volume, litresPerSecond, deltaTime);
            return Mathf.Min(drawn, AtmosPortConstants.FilterRatedFlowMolesPerSecond * deltaTime) / inletMoles;
        }

        [Server]
        public void ServerSetFlowLitresPerSecond(int litresPerSecond)
        {
            _flowLitresPerSecond = Mathf.Clamp(litresPerSecond, 0, AtmosPortConstants.FilterMaxLitresPerSecond);
        }

        [Server]
        public void ServerToggleGas(GasId gasId)
        {
            _selectedGases ^= GasBit(gasId);
        }

        /// <summary>
        /// One tick's work, split out so tests can drive it without a tile map.
        /// </summary>
        public float Filter(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord inlet,
            GasPipeNetworkRecord outlet,
            GasPipeNetworkRecord side,
            float deltaTime)
        {
            LastStalled = IsStalled(
                outlet.GetPressure(AtmosConstants.DefaultGasCount),
                side.GetPressure(AtmosConstants.DefaultGasCount));

            if (LastStalled)
                return 0f;

            float fraction = ComputeFraction(inlet, _flowLitresPerSecond, deltaTime);
            return registry.MoveGases(inlet, side, _selectedGases, fraction)
                + registry.MoveGases(inlet, outlet, AllGases & ~_selectedGases, fraction);
        }

        protected override bool RunThreeWayTick(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord back,
            GasPipeNetworkRecord front,
            GasPipeNetworkRecord side,
            float deltaTime) =>
            RecordFlow(Filter(registry, back, front, side, deltaTime), deltaTime);

        private static ulong GasBit(GasId gasId) => 1UL << gasId.Value;
    }
}
