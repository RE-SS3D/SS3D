using FishNet.Object;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pulls filtered gases from the turf cell into the connected pipe network.
    /// </summary>
    public sealed class ScrubberController : AtmosPortControllerBase
    {
        private static readonly int ScrubActiveId = Animator.StringToHash("scrubActive");

        private bool _filterO2 = true;
        private bool _filterN2 = true;
        private bool _filterCo2 = true;
        private bool _filterPlasma;
        private bool _filterToxins = true;

        public void GetFilterStates(
            out bool filterO2,
            out bool filterN2,
            out bool filterCo2,
            out bool filterPlasma,
            out bool filterToxins)
        {
            filterO2 = _filterO2;
            filterN2 = _filterN2;
            filterCo2 = _filterCo2;
            filterPlasma = _filterPlasma;
            filterToxins = _filterToxins;
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
                    _filterPlasma = enabled;
                    break;
                case 4:
                    _filterToxins = enabled;
                    break;
            }
        }

        [Server]
        public void ServerSetFilters(bool o2, bool n2, bool co2, bool plasma, bool toxins)
        {
            _filterO2 = o2;
            _filterN2 = n2;
            _filterCo2 = co2;
            _filterPlasma = plasma;
            _filterToxins = toxins;
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
            float budgetMoles = AtmosPortConstants.ScrubberRatedFlowMolesPerSecond * pressureFactor * deltaTime;

            if (budgetMoles <= 0f)
                return false;

            bool movedAny = false;
            float remainingBudget = budgetMoles;

            foreach (GasId gasId in GetActiveFilteredGases())
            {
                if (remainingBudget <= 0f)
                    break;

                if (!turfSimulation.TryGetGasMoles(turfCell, gasId, out float available) || available <= 0f)
                    continue;

                float request = Mathf.Min(remainingBudget, available);
                if (pipeSimulation.TryTransferMoles(
                        networkId,
                        gasId,
                        request,
                        turfCell,
                        PipeTransferDirection.ToNetwork,
                        out float moved)
                    && moved > 0f)
                {
                    remainingBudget -= moved;
                    movedAny = true;
                }
            }

            return movedAny;
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
                3 => _filterPlasma,
                4 => _filterToxins,
                _ => false,
            };
        }

        private IEnumerable<GasId> GetActiveFilteredGases()
        {
            if (_filterO2)
            {
                yield return AtmosConstants.Oxygen;
            }

            if (_filterN2)
            {
                yield return AtmosConstants.Nitrogen;
            }

            if (_filterCo2)
            {
                yield return AtmosConstants.CarbonDioxide;
            }

            // "Toxins" is currently treated as an alias of plasma in the core gas set.
            if (_filterPlasma || _filterToxins)
            {
                yield return AtmosConstants.Plasma;
            }
        }
    }
}
