using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Networking
{
    public static class StoredItemReaderWriter
    {
        public static void WriteStoredItem(this NetworkWriter writer, Container.StoredItem item)
        {
            writer.WriteGameObject(item.Item.gameObject);
            writer.WriteVector2Int(item.Position);
        }
        
        public static Container.StoredItem ReadStoredItem(this NetworkReader reader)
        {
            var item = reader.ReadGameObject().GetComponent<Item>();
            Vector2Int position = reader.ReadVector2Int();
            return new Container.StoredItem(item, position);
        }
    }
}