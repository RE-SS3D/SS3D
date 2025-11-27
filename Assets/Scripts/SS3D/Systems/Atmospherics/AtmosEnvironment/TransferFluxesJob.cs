using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct TransferFluxesJob : IJob
    {
        [ReadOnly]
        private readonly NativeArray<AtmosObjectNeighboursIndexes> _neighboursIndexes;

        [ReadOnly]
        private NativeArray<MoleTransferToNeighbours> _moleTransfers;

        private NativeArray<AtmosObject> _tileObjectBuffer;

        [ReadOnly]
        private NativeArray<int> _activeIndexes;

        public TransferFluxesJob(
            NativeArray<MoleTransferToNeighbours> moleTransfers,
            NativeArray<AtmosObject> tileObjectBuffer,
            NativeArray<AtmosObjectNeighboursIndexes> neighboursIndexes,
            NativeArray<int> activeIndexes)
        {
            _moleTransfers = moleTransfers;
            _tileObjectBuffer = tileObjectBuffer;
            _activeIndexes = activeIndexes;
            _neighboursIndexes = neighboursIndexes;
        }

        public void Execute()
        {
            for (int i = 0; i < _activeIndexes.Length; i++)
            {
                int activeIndex = _activeIndexes[i];
                MoleTransferToNeighbours transfer = _moleTransfers[activeIndex];
                int atmosObjectFromIndex = transfer.IndexFrom;
                AtmosObject atmosObject = _tileObjectBuffer[atmosObjectFromIndex];
                atmosObject.RemoveCoreGasses(transfer.TransferMolesNorth);
                atmosObject.RemoveCoreGasses(transfer.TransferMolesSouth);
                atmosObject.RemoveCoreGasses(transfer.TransferMolesEast);
                atmosObject.RemoveCoreGasses(transfer.TransferMolesWest);

                _tileObjectBuffer[atmosObjectFromIndex] = atmosObject;

                int neighbourNorthIndex = _neighboursIndexes[activeIndex].NorthNeighbour;
                int neighbourSouthIndex = _neighboursIndexes[activeIndex].SouthNeighbour;
                int neighbourEastIndex = _neighboursIndexes[activeIndex].EastNeighbour;
                int neighbourWestIndex = _neighboursIndexes[activeIndex].WestNeighbour;

                TransferToNeighbour(neighbourNorthIndex, transfer.TransferMolesNorth);
                TransferToNeighbour(neighbourSouthIndex, transfer.TransferMolesSouth);
                TransferToNeighbour(neighbourEastIndex, transfer.TransferMolesEast);
                TransferToNeighbour(neighbourWestIndex, transfer.TransferMolesWest);
            }
        }

        private void TransferToNeighbour(int neighbourIndex, float4 transfer)
        {
            if (neighbourIndex == -1)
            {
                return;
            }

            AtmosObject neighbour = _tileObjectBuffer[neighbourIndex];
            neighbour.AddCoreGasses(transfer);
            _tileObjectBuffer[neighbourIndex] = neighbour;
        }
    }
}
