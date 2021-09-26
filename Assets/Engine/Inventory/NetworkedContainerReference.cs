using Mirror;
using System.Collections.Generic;
using UnityEngine;


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
            var sync = container.GetComponents<ContainerDescriptor>();
            ContainerDescriptor target = null;
            if (sync == null)
            {
                return null;
            }

            int index = 0;
            foreach(ContainerDescriptor c in sync)
            {
                if(c.attachedContainer == container)
                {
                    target = c;
                    break;
                }
                index += 1;
            }

            if(target == null)
            {
                return null;
            }

            return new NetworkedContainerReference
            {
                SyncNetworkId = target.netId,
                ContainerIndex = (uint) index
            };
        }
        
        public AttachedContainer FindContainer()
        {
            if (NetworkIdentity.spawned.TryGetValue(SyncNetworkId, out NetworkIdentity identity))
            {
                var sync = identity.gameObject.GetComponents<ContainerDescriptor>();
                if (sync == null)
                {
                    return null;
                }

                if (ContainerIndex < sync.Length)
                {
                    return sync[ContainerIndex].attachedContainer;
                }
            }

            Debug.Log("can't get value in FindContainer()");

            return null;
        }
    }
}