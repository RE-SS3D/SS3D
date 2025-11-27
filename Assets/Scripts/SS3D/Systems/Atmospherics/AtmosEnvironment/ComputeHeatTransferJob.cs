using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Compute how much heat should be transferred each atmos tick to each neighbour.
    /// </summary>
    public struct ComputeHeatTransferJob : IJobParallelFor
    {
            // Array containing all atmos tiles of the map.
            [ReadOnly]
            private readonly NativeArray<AtmosObject> _tileObjectBuffer;

            // Array containing the neighbour indexes of all atmos tiles of the map. At index i, contains the neighbour of atmos tile at index i in _nativeAtmosTiles array.
            [ReadOnly]
            private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;

            // Array containing the indexes of all active atmos tiles.
            [ReadOnly]
            private readonly NativeArray<int> _activeIndexes;

            private readonly float _deltaTime;

            // Array of all heat transfer to neighbours
            [NativeDisableParallelForRestriction]
            [WriteOnly]
            private NativeArray<HeatTransferToNeighbours> _heatTransfers;

            public ComputeHeatTransferJob(
                NativeArray<AtmosObject> tileObjectBuffer,
                NativeArray<HeatTransferToNeighbours> heatTransfers,
                NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes,
                NativeArray<int> activeIndexes,
                float deltaTime)
            {
                _tileObjectBuffer = tileObjectBuffer;
                _deltaTime = deltaTime;
                _heatTransfers = heatTransfers;
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

                _heatTransfers[activeIndex] = AtmosCalculator.SimulateTemperature(
                    _tileObjectBuffer[activeIndex],
                    activeIndex,
                    northNeighbour,
                    southNeighbour,
                    eastNeighbour,
                    westNeighbour,
                    _deltaTime,
                    hasNeighbours);
            }
    }
}
