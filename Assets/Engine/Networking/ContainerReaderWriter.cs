using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Networking
{
    public static class ContainerReaderWriter
    {
        public static void WriteContainer(this NetworkWriter writer, Container container)
        {
            writer.WriteVector2Int(container.Size);
            var items = container.StoredItems;
            int itemCount = items.Count;
            writer.WriteUInt16((ushort) itemCount);
            for (var i = 0; i < itemCount; i++)
            {
                writer.WriteStoredItem(items[i]);
            }
        }
     
        public static Container ReadContainer(this NetworkReader reader)
        {
            Vector2Int size = reader.ReadVector2Int();
            ushort count = reader.ReadUInt16();
            var container = new Container {Size = size};
            container.StoredItems.Capacity = count;
            for (var i = 0; i < count; i++)
            {
                container.StoredItems.Add(reader.ReadStoredItem());
            }

            return container;
        }
    }
}