using Mirror;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// Represents a container that can be found by the server and client
    /// </summary>
    public struct NetworkedContainerReference
    {
        /// <summary>
        /// The network id of the game object containing the network sync
        /// </summary>
        public uint SyncNetworkId;
        /// <summary>
        /// The index of the container in the game object
        /// </summary>
        public uint ContainerIndex;

        public static NetworkedContainerReference? CreateReference(AttachedContainer container)
        {
            var sync = container.GetComponentInParent<ContainerSync>();
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
                SyncNetworkId = sync.netId,
                ContainerIndex = (uint) index
            };
        }
        
        public AttachedContainer FindContainer()
        {
            if (NetworkIdentity.spawned.TryGetValue(SyncNetworkId, out NetworkIdentity identity))
            {
                var sync = identity.gameObject.GetComponent<ContainerSync>();
                if (sync == null)
                {
                    return null;
                }

                if (ContainerIndex < sync.Containers.Count)
                {
                    return sync.Containers[(int) ContainerIndex];
                }
            }

            return null;
        }
    }
}