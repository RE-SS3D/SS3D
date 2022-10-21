using System.Collections.Generic;
using FishNet.Object;
using SS3D.Storage.Containers;

namespace SS3D.Storage
{
    /// <summary>
    /// Represents a container that can be found by the server and client
    /// </summary>
    public struct NetworkedContainerReference
    {
        /// <summary>
        /// The network id of the game object containing the network sync
        /// </summary>
        public int SyncNetworkId;
        /// <summary>
        /// The index of the container in the game object
        /// </summary>
        public uint ContainerIndex;

        public static NetworkedContainerReference? CreateReference(AttachedContainer container)
        {
            ContainerSync sync = container.GetComponentInParent<ContainerSync>();
            if (sync == null)
            {
                return null;
            }

            int index = sync.IndexOf(container);
            if (index == -1)
            {
                return null;
            }

            return new NetworkedContainerReference
            {
                SyncNetworkId = sync.ObjectId,
                ContainerIndex = (uint) index
            };
        }
        
        public AttachedContainer FindContainer()
        {
            Dictionary<int,NetworkObject> spawned = FishNet.InstanceFinder.ServerManager.Objects.Spawned;

            if (!spawned.TryGetValue(SyncNetworkId, out NetworkObject identity))
            {
                return null;
            }

            ContainerSync sync = identity.GetComponent<ContainerSync>();
            if (sync == null)
            {
                return null;
            }

            if (ContainerIndex < sync.Containers.Count)
            {
                return sync.Containers[(int) ContainerIndex];
            }

            return null;
        }
    }
}