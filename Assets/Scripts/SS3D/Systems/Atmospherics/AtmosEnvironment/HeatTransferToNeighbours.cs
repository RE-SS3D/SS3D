using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Small struct to keep track of how much heat to transfer to each neighbour
    /// </summary>
    public readonly struct HeatTransferToNeighbours
    {
        /// <summary>
        /// Index of the atmos container from which to transfer, from the NativeArray NativeAtmosTiles in the AtmosJobPersistentData struct.
        /// </summary>
        public readonly int IndexFrom;

        /// <summary>
        /// neighbour transfer in north south east west order.
        /// </summary>
        public readonly float4 TransferHeat;

        public HeatTransferToNeighbours(int indexFrom, float4 transferHeat)
        {
            IndexFrom = indexFrom;
            TransferHeat = transferHeat;
        }
    }
}
