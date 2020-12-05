using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Networking
{
    public static class NetworkedContainerReferenceReaderWriter
    {
        public static void WriteNetworkedContainerReference(this NetworkWriter writer, NetworkedContainerReference container)
        {
            writer.WritePackedUInt32(container.SyncNetworkId);
            writer.WritePackedUInt32(container.ContainerIndex);
        }
        
        public static NetworkedContainerReference ReadNetworkedContainerReference(this NetworkReader reader)
        {
            uint networkId = reader.ReadPackedUInt32();
            uint index = reader.ReadPackedUInt32();
            return new NetworkedContainerReference
            {
                SyncNetworkId = networkId,
                ContainerIndex = index
            };
        }
    }
}