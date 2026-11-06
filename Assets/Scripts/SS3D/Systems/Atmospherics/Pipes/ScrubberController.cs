using FishNet.Object.Synchronizing;
using FishNet.Object;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Audio;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pulls filtered gases from the turf cell into the connected pipe network.
    /// </summary>
    public sealed class ScrubberController : AtmosPortControllerBase, IAmbientLoopState
    {
        private static readonly int ScrubActiveId = Animator.StringToHash("scrubActive");

        [SyncVar]
        private int _flowRate = AtmosPortConstants.ScrubberNormalFlowRate;

        // A chamber intake carries air, where a room scrubber must leave air alone.
        [SerializeField]
        private bool _isAirIntake;

        private bool _filterO2;
        private bool _filterN2;
        private bool _filterCo2 = true;
        private bool _filterToxins;

        public int FlowRate => _flowRate;

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerApplyNormalSettings();
        }

        /// <summary>
        /// Back to this scrubber's everyday speed and filters.
        /// </summary>
        [Server]
        public void ServerApplyNormalSettings()
        {
            _flowRate = AtmosPortConstants.ScrubberNormalFlowRate;
            ServerSetFilters(o2: _isAirIntake, n2: _isAirIntake, co2: !_isAirIntake, toxins: false);
        }

        /// <summary>
        /// Flat out on every gas, to siphon a room.
        /// </summary>
        [Server]
        public void ServerApplyPanicSettings()
        {
            _flowRate = AtmosPortConstants.ScrubberMaxFlowRate;
            ServerSetFilters(o2: true, n2: true, co2: true, toxins: true);
        }

        public static float GetRatedFlowMolesPerSecond(int flowRate)
        {
            float scale = Mathf.Clamp(flowRate, 1, AtmosPortConstants.ScrubberMaxFlowRate) / (float)AtmosPortConstants.ScrubberMaxFlowRate;
            return AtmosPortConstants.ScrubberRatedFlowMolesPerSecond * scale;
        }

        public void GetFilterStates(
            out bool filterO2,
            out bool filterN2,
            out bool filterCo2,
            out bool filterToxins)
        {
            filterO2 = _filterO2;
            filterN2 = _filterN2;
            filterCo2 = _filterCo2;
            filterToxins = _filterToxins;
        }

        [Server]
        public void ServerSetFlowRate(int flowRate)
        {
            _flowRate = Mathf.Clamp(flowRate, 1, AtmosPortConstants.ScrubberMaxFlowRate);
        }

        [Server]
        public void ServerToggleFilter(int filterIndex)
        {
            bool current = GetFilterEnabled(filterIndex);
            ServerSetFilter(filterIndex, !current);
        }

        [Server]
        public void ServerSetFilter(int filterIndex, bool enabled)
        {
            switch (filterIndex)
            {
                case 0:
                    _filterO2 = enabled;
                    break;
                case 1:
                    _filterN2 = enabled;
                    break;
                case 2:
                    _filterCo2 = enabled;
                    break;
                case 3:
                    _filterToxins = enabled;
                    break;
            }
        }

        [Server]
        public void ServerSetFilters(bool o2, bool n2, bool co2, bool toxins)
        {
            _filterO2 = o2;
            _filterN2 = n2;
            _filterCo2 = co2;
            _filterToxins = toxins;
        }

        public override TileLayer[] PipeLayers => AtmosDevicePipeResolver.ScrubberLayers;

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
            float turfPressure = turfSimulation.GetCellPressure(turfCell);
            float networkPressure = network.GetPressure(AtmosConstants.DefaultGasCount);
            // Scrubbers actively pump gas from turf into the connected network; they are not passive valves.
            // Allow flow at the rated rate, tapering as the connected network pressure approaches/exceeds
            // the turf pressure within the supported differential.
            float maxDiff = Mathf.Max(AtmosPortConstants.PortMaxDifferentialKpa, 1e-3f);
            float differential = turfPressure - networkPressure;
            if (differential <= -maxDiff)
            {
                return false;
            }

            float pressureFactor = Mathf.Clamp01((differential + maxDiff) / maxDiff);
            float ratedFlow = _isAirIntake ? AtmosPortConstants.AirIntakeRatedFlowMolesPerSecond : GetRatedFlowMolesPerSecond(_flowRate);
            float budgetMoles = ratedFlow * pressureFactor * deltaTime;

            if (budgetMoles <= 0f)
                return false;

            ulong gases = SelectedGases(pipeSimulation.ToxicGases);
            int gasCount = pipeSimulation.GasTypeCount;
            float selected = 0f;
            for (int gasId = 0; gasId < gasCount; gasId++)
            {
                if ((gases & (1UL << gasId)) != 0UL
                    && turfSimulation.TryGetGasMoles(turfCell, new GasId((ushort)gasId), out float moles))
                {
                    selected += moles;
                }
            }

            if (selected <= 0f)
                return false;

            // Each gas moves in proportion to its share of the tile, so an intake carries the mix it
            // sits in and a trace of CO₂ isn't starved by the air around it.
            float fraction = Mathf.Min(budgetMoles / selected, 1f);

            // A mask, not yield/IEnumerable: an iterator allocates per scrubber per tick.
            bool movedAny = false;
            for (int gasId = 0; gasId < gasCount; gasId++)
            {
                if ((gases & (1UL << gasId)) != 0UL)
                    movedAny |= TryScrubGas(pipeSimulation, turfSimulation, networkId, turfCell, new GasId((ushort)gasId), fraction);
            }

            return movedAny;
        }

        private ulong SelectedGases(ulong toxicGases)
        {
            ulong gases = _filterToxins ? toxicGases : 0UL;
            if (_filterO2)
                gases |= 1UL << AtmosConstants.Oxygen.Value;

            if (_filterN2)
                gases |= 1UL << AtmosConstants.Nitrogen.Value;

            if (_filterCo2)
                gases |= 1UL << AtmosConstants.CarbonDioxide.Value;

            return gases;
        }

        private static bool TryScrubGas(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            GasPipeNetworkId networkId,
            TileCoord turfCell,
            GasId gasId,
            float fraction)
        {
            return turfSimulation.TryGetGasMoles(turfCell, gasId, out float available)
                && available > 0f
                && pipeSimulation.TryTransferMoles(
                    networkId,
                    gasId,
                    available * fraction,
                    turfCell,
                    PipeTransferDirection.ToNetwork,
                    out float moved)
                && moved > 0f;
        }

        protected override void ApplyDeviceSpecificAnimatorState(bool flowing)
        {
            if (_animator == null)
                return;

            _animator.SetBool(ScrubActiveId, flowing);
        }

        private bool GetFilterEnabled(int filterIndex)
        {
            return filterIndex switch
            {
                0 => _filterO2,
                1 => _filterN2,
                2 => _filterCo2,
                3 => _filterToxins,
                _ => false,
            };
        }

    }
}
