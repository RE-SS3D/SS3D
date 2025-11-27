using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Compute space winds.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ComputeVelocityJob : IJobParallelFor
    {
        [ReadOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        [NativeDisableParallelForRestriction]
        private NativeArray<AtmosObject> _tileObjectBuffer;

        [ReadOnly]
        private NativeArray<int> _activeIndexes;

        public ComputeVelocityJob(
            NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<MoleTransferToNeighbours> moleTransfers,
            NativeArray<int> activeIndexes)
        {
            _tileObjectBuffer = tileObjectBuffer;
            _moleTransfers = moleTransfers;
            _activeIndexes = activeIndexes;
        }

        public void Execute(int index)
        {
            // only compute winds for active tiles
            int activeIndex = _activeIndexes[index];
            AtmosObject atmosObject = _tileObjectBuffer[activeIndex];

            // Currently velocity is simply the amount of moles transferred to each neighbour.
            atmosObject.VelocityNorth = _moleTransfers[activeIndex].TransferMolesNorth;
            atmosObject.VelocitySouth = _moleTransfers[activeIndex].TransferMolesSouth;
            atmosObject.VelocityEast = _moleTransfers[activeIndex].TransferMolesEast;
            atmosObject.VelocityWest = _moleTransfers[activeIndex].TransferMolesWest;
            _tileObjectBuffer[activeIndex] = atmosObject;
        }
    }
}
