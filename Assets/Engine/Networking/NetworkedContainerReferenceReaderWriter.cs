using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Networking
{
    public static class NetworkedContainerReferenceReaderWriter
    {
        public static void WriteNetworkedContainerReference(this NetworkWriter writer, NetworkedContainerReference container)
        {
            writer.WriteUInt(container.SyncNetworkId);
            writer.WriteUInt(container.ContainerIndex);
        }
        
        public static NetworkedContainerReference ReadNetworkedContainerReference(this NetworkReader reader)
        {
            uint networkId = reader.ReadUInt();
            uint index = reader.ReadUInt();
            return new NetworkedContainerReference
            {
                SyncNetworkId = networkId,
                ContainerIndex = index
            };
        }
    }
}