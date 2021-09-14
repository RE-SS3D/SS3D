using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Networking
{
    public static class StoredItemReaderWriter
    {
        public static void WriteStoredItem(this NetworkWriter writer, Container.StoredIContainerizable item)
        {
            writer.WriteGameObject(item.Item.GetGameObject());
            writer.WriteVector2Int(item.Position);
        }
        
        public static Container.StoredIContainerizable ReadStoredItem(this NetworkReader reader)
        {
            var item = reader.ReadGameObject().GetComponent<Item>();
            Vector2Int position = reader.ReadVector2Int();
            return new Container.StoredIContainerizable(item, position);
        }
    }
}