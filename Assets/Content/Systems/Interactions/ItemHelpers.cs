using Mirror;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public static class ItemHelpers
    {
        /// <summary>
        /// Destroys an item
        /// </summary>
        /// <param name="item">The item to destroy</param>
        public static void DestroyItem(Item item)
        {
            if (item.container != null)
            {
                item.container.RemoveItem(item.GameObject);
            }
            
            NetworkServer.Destroy(item.GameObject);
        }

        /// <summary>
        /// Replaces an item with a different item
        /// </summary>
        /// <param name="original">The original item</param>
        /// <param name="replacement">The replacement</param>
        /// <param name="destroyOriginal">If the original item should be destroyed</param>
        public static void ReplaceItem(Item original, Item replacement, bool destroyOriginal = true)
        {
            Container container = original.container;
            if (container != null)
            {
                int slot = container.GetSlotFromItem(original);
                container.RemoveItem(slot);
                container.AddItem(slot, replacement.gameObject);
            }
            else
            {
                replacement.transform.position = original.transform.position;
                replacement.transform.rotation = original.transform.rotation;
            }

            if (destroyOriginal)
            {
                DestroyItem(original);
            }
        }

        public static Item CreateItem(GameObject prefab)
        {
            GameObject gameObject = Object.Instantiate(prefab);
            NetworkServer.Spawn(gameObject);
            return gameObject.GetComponent<Item>();
        }
    }
    
    
}