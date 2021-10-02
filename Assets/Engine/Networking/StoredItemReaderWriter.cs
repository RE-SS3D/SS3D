using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Networking
{
    public static class StoredItemReaderWriter
    {
        public static void WriteStoredItem(this NetworkWriter writer, Container.StoredIContainable containable)
        {
            writer.WriteGameObject(containable.Containable.GetGameObject());
            writer.WriteVector2Int(containable.Position);
        }
        
        public static Container.StoredIContainable ReadStoredItem(this NetworkReader reader)
        {
            var containable = reader.ReadGameObject().GetComponent<Item>();
            Vector2Int position = reader.ReadVector2Int();
            return new Container.StoredIContainable(containable, position);
        }
    }
}