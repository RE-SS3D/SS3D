using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Small struct to keep track of how much moles of each gas to transfer to each neighbour
    /// </summary>
    public readonly struct MoleTransferToNeighbours
    {
        /// <summary>
        /// Index of the atmos container from which to transfer, from the NativeArray NativeAtmosTiles in the AtmosJobPersistentData struct.
        /// </summary>
        public readonly int IndexFrom;

        // gas transfer in moles to each neighbours.
        public readonly float4 TransferMolesNorth;
        public readonly float4 TransferMolesSouth;
        public readonly float4 TransferMolesEast;
        public readonly float4 TransferMolesWest;

        public MoleTransferToNeighbours(int indexFrom, float4 transferMolesNorth, float4 transferMolesSouth, float4 transferMolesEast, float4 transferMolesWest)
        {
            IndexFrom = indexFrom;
            TransferMolesNorth = transferMolesNorth;
            TransferMolesSouth = transferMolesSouth;
            TransferMolesEast = transferMolesEast;
            TransferMolesWest = transferMolesWest;
        }
    }
}
