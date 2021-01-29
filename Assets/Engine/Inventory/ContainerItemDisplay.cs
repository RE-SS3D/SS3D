using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Inventory
{
    public class ContainerItemDisplay : MonoBehaviour
    {
        public Transform[] Displays;
        public AttachedContainer Container;

        private Item[] displayedItems;

        public void Start()
        {
            Assert.IsNotNull(Container);
            
            displayedItems = new Item[Displays.Length];
            Container.ItemAttached += ContainerOnItemAttached;
            Container.ItemDetached += ContainerOnItemDetached;
        }

        public void OnDestroy()
        {
            Container.ItemAttached -= ContainerOnItemAttached;
        }

        private void ContainerOnItemAttached(object sender, Item item)
        {
            int index = -1;
            for (var i = 0; i < Displays.Length; i++)
            {
                if (displayedItems[i] == null)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return;
            }

            Transform itemTransform = item.transform;

            // Check if a custom attachment point should be used
            Transform attachmentPoint = item.attachmentPoint;
            if (attachmentPoint != null)
            {
                // Create new (temporary) point
                // HACK: Required because rotation pivot can be different
                GameObject temporaryPoint = new GameObject("TempPivotPoint");
                
                temporaryPoint.transform.SetParent(Displays[index].transform, false);
                temporaryPoint.transform.localPosition = Vector3.zero;
                temporaryPoint.transform.rotation = attachmentPoint.root.rotation *  attachmentPoint.localRotation;
                
                // Assign parent
                itemTransform.SetParent(temporaryPoint.transform, false);
                // Assign the relative position between the attachment point and the object
                itemTransform.localPosition = -attachmentPoint.localPosition;
                //item.transform.rotation = displays[index].transform.rotation;
                itemTransform.localRotation = Quaternion.identity;
            }
            else
            {
                itemTransform.SetParent(Displays[index].transform, false);
                itemTransform.localPosition = new Vector3();
                itemTransform.localRotation = new Quaternion();
            }

            displayedItems[index] = item;
        }
        
        private void ContainerOnItemDetached(object sender, Item item)
        {
            int index = -1;
            for (var i = 0; i < Displays.Length; i++)
            {
                if (displayedItems[i] == item)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return;
            }

            Transform itemParent = item.transform.parent;
            if (itemParent != null && itemParent != Displays[index])
            {
                item.transform.SetParent(null, true);
                Destroy(itemParent.gameObject);
            }

            displayedItems[index] = null;
        }
    }
}