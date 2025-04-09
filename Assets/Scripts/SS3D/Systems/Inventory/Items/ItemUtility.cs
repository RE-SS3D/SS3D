using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public static class ItemUtility
    {
        public static void Place(Item item, Vector3 position, Quaternion rotation, Transform lookTarget = null)
        {
            Vector3 itemDimensions = item.GetComponentInChildren<Collider>().bounds.size;
            float itemSize = 0;

            for (int i = 0; i < 3; i++)
            {
                if (itemDimensions[i] > itemSize)
                {
                    itemSize = itemDimensions[i];
                }
            }

            float distance = Vector3.Distance(item.transform.position, position);
            position = distance > 0 ? position + new Vector3(0, itemSize * 0.5f, 0) : position;

            if (lookTarget && distance > 0)
            {
                item.transform.LookAt(lookTarget);
            }
            else
            {
                item.transform.rotation = rotation;
            }

            item.transform.position = position;
        }
    }
}