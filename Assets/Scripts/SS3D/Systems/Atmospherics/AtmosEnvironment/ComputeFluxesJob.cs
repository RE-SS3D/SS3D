using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Job to compute both active and diffuse fluxes. Active fluxes are flux happening with pressure difference.
    /// Diffuse fluxes are fluxes happening when there's no pressure differences, but there's differences in moles.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ComputeFluxesJob : IJobParallelFor
    {
        // Array containing all atmos tiles of the map.
        [ReadOnly]
        private readonly NativeArray<AtmosObject> _tileObjectBuffer;

        // Array containing the neighbour indexes of all atmos tiles of the map. At index i, contains the neighbour of atmos tile at index i in _nativeAtmosTiles array.
        [ReadOnly]
        private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;

        // Array containing all active (when the job compute active fluxes) or semi active (when the job computes semi active fluxes) atmos tiles of the map.
        [ReadOnly]
        private readonly NativeArray<int> _activeIndexes;

        private readonly float _deltaTime;

        // If the fluxes computed should be active fluxes or diffuse fluxes.
        private readonly bool _activeFlux;

        [NativeDisableParallelForRestriction]
        [WriteOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        public ComputeFluxesJob(
            NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<MoleTransferToNeighbours> moleTransfers,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes,
            NativeArray<int> activeIndexes,
            float deltaTime,
            bool activeFlux)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _deltaTime = deltaTime;
            _moleTransfers = moleTransfers;
            _activeFlux = activeFlux;
            _neighboursIndexes = neighboursIndexes;
            _activeIndexes = activeIndexes;
        }

        public void Execute(int index)
        {
            int activeIndex = _activeIndexes[index];

            AtmosObject defaultAtmos = default;

            AtmosObject northNeighbour = _neighboursIndexes[activeIndex].NorthNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].NorthNeighbour];
            AtmosObject southNeighbour = _neighboursIndexes[activeIndex].SouthNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].SouthNeighbour];
            AtmosObject eastNeighbour = _neighboursIndexes[activeIndex].EastNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].EastNeighbour];
            AtmosObject westNeighbour = _neighboursIndexes[activeIndex].WestNeighbour == -1 ? defaultAtmos : _tileObjectBuffer[_neighboursIndexes[activeIndex].WestNeighbour];

            bool4 hasNeighbours = new(
                _neighboursIndexes[activeIndex].NorthNeighbour != -1,
                _neighboursIndexes[activeIndex].SouthNeighbour != -1,
                _neighboursIndexes[activeIndex].EastNeighbour != -1,
                _neighboursIndexes[activeIndex].WestNeighbour != -1);

            // Do actual work
            _moleTransfers[activeIndex] = AtmosCalculator.SimulateGasTransfers(
                _tileObjectBuffer[activeIndex],
                activeIndex,
                northNeighbour,
                southNeighbour,
                eastNeighbour,
                westNeighbour,
                _deltaTime,
                _activeFlux,
                hasNeighbours);
        }
    }
}
