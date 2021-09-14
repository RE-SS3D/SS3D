using UnityEngine;

namespace SS3D.Engine.Inventory
{
    public static class ItemUtility
    {
        public static void Place(IContainerizable item, Vector3 position, Quaternion rotation, Transform lookTarget = null)
        {
            Vector3 itemDimensions = item.GetGameObject().GetComponentInChildren<Collider>().bounds.size;
            float itemSize = 0;

            for (int i = 0; i < 3; i++)
            {
                if (itemDimensions[i] > itemSize)
                {
                    itemSize = itemDimensions[i];
                }
            }

            float distance = Vector3.Distance(item.GetGameObject().transform.position, position);
            position = distance > 0 ? position + new Vector3(0, itemSize * 0.5f, 0) : position;

            if (distance > 0)
            {
                item.GetGameObject().transform.LookAt(lookTarget);
            }
            else
            {
                item.GetGameObject().transform.rotation = rotation;
            }

            item.GetGameObject().transform.position = position;
        }
    }
}