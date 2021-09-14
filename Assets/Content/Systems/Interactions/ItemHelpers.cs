using System;
using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Content.Systems.Interactions
{
    public static class ItemHelpers
    {
        /// <summary>
        /// Replaces an item with a different item
        /// </summary>
        /// <param name="original">The original item</param>
        /// <param name="replacement">The replacement</param>
        /// <param name="destroyOriginal">If the original item should be destroyed</param>
        public static void ReplaceItem(IContainerizable original, Item replacement, bool destroyOriginal = true)
        {
            Container container = original.Container;
            if (container != null)
            {
                int index = container.FindItem(original);
                Container.StoredIContainerizable storedItem = container.StoredContainerizables[index];
                container.RemoveItem(storedItem.Item);
                container.AddItem(replacement, storedItem.Position);
            }
            else
            {
                replacement.transform.position = original.GetGameObject().transform.position;
                replacement.transform.rotation = original.GetGameObject().transform.rotation;
            }

            if (destroyOriginal)
            {
                original.Destroy();
            }
        }

        public static Item CreateItem(GameObject prefab)
        {
            GameObject gameObject = Object.Instantiate(prefab);
            NetworkServer.Spawn(gameObject);
            var item = gameObject.GetComponent<Item>();
            if (NetworkClient.active)
            {
                // Render the preview right after creation
                Sprite unused = item.InventorySprite;
            }
            return item;
        }
    }
}