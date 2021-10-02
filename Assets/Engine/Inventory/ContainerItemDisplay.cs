using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// This allows control over the position of displayed items inside the container.
    /// It also allows to define multiple points where items can be displayed inside the container,
    /// and items placed in the container appears at those different points in the order defined. 
    /// Take for example a battery compartment, battery should appear side by side when placed inside the compartment container.
    /// Without this they would pile up in the same spot.
    /// </summary>
    public class ContainerItemDisplay : MonoBehaviour
    {
        public ContainerDescriptor containerDescriptor;
        public bool Mirrored;

        /// <summary>
        /// The list of items displayed in the container;
        /// </summary>
        private Item[] displayedItems;

        public void Start()
        {
            Assert.IsNotNull(containerDescriptor);
            
            displayedItems = new Item[containerDescriptor.displays.Length];
            containerDescriptor.attachedContainer.ItemAttached += ContainerOnItemAttached;
            containerDescriptor.attachedContainer.ItemDetached += ContainerOnItemDetached;
        }

        public void OnDestroy()
        {
            containerDescriptor.attachedContainer.ItemAttached -= ContainerOnItemAttached;
            containerDescriptor.attachedContainer.ItemDetached -= ContainerOnItemDetached;
        }

        private void ContainerOnItemAttached(object sender, Item item)
        {
            // Defines the transform of the item to be the first available position.
            int index = -1;
            for (var i = 0; i < containerDescriptor.displays.Length; i++)
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
            if (Mirrored && item.attachmentPointAlt != null)
            {
                attachmentPoint = item.attachmentPointAlt;
            }

            if (attachmentPoint != null)
            {
                // Create new (temporary) point
                // HACK: Required because rotation pivot can be different
                GameObject temporaryPoint = new GameObject("TempPivotPoint");
                
                temporaryPoint.transform.SetParent(containerDescriptor.displays[index].transform, false);
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
                itemTransform.SetParent(containerDescriptor.displays[index].transform, false);
                itemTransform.localPosition = new Vector3();
                itemTransform.localRotation = new Quaternion();
            }

            displayedItems[index] = item;
        }
        
        private void ContainerOnItemDetached(object sender, Item item)
        {
            int index = -1;
            for (var i = 0; i < containerDescriptor.displays.Length; i++)
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
            if (itemParent != null && itemParent != containerDescriptor.displays[index])
            {
                item.transform.SetParent(null, true);
                Destroy(itemParent.gameObject);
            }

            displayedItems[index] = null;
        }
    }
}