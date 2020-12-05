using System;
using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Networking
{
    public static class AttachedContainerReaderWriter
    {
        public static void WriteAttachedContainer(this NetworkWriter writer, AttachedContainer container)
        {
            var reference = NetworkedContainerReference.CreateReference(container) ??
                            throw new InvalidOperationException("Cannot create reference for attached container");
            writer.WriteNetworkedContainerReference(reference);
        }
        
        public static AttachedContainer ReadAttachedContainer(this NetworkReader reader)
        {
            NetworkedContainerReference reference = reader.ReadNetworkedContainerReference();
            return reference.FindContainer();
        }
    }
}