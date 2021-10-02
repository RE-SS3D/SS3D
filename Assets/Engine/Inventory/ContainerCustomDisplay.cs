using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;


namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// This allows control over the position of displayed items inside the container.
    /// It also allows to define multiple points where items can be displayed inside the container,
    /// and items placed in the container appears at those different points in the order defined. 
    /// Take for example a battery compartment, battery should appear side by side when placed inside the compartment container.
    /// Without this they would pile up in the same spot.
    /// </summary>
    public class ContainerCustomDisplay : MonoBehaviour
    {
        public ContainerDescriptor containerDescriptor;
        public bool Mirrored;

        /// <summary>
        /// The list of items displayed in the container;
        /// </summary>
        private List<IContainable> displayedItems;

        public void Start()
        {
            Assert.IsNotNull(containerDescriptor);
            
            displayedItems = new List<IContainable>();
            containerDescriptor.attachedContainer.ItemAttached += ContainerOnItemAttached;
            containerDescriptor.attachedContainer.ItemDetached += ContainerOnItemDetached;
        }

        public void OnDestroy()
        {
            containerDescriptor.attachedContainer.ItemAttached -= ContainerOnItemAttached;
            containerDescriptor.attachedContainer.ItemDetached -= ContainerOnItemDetached;
        }

        private void ContainerOnItemAttached(object sender, IContainable containable)
        {
            // Defines the transform of the containable to be the first available position.
            int index = displayedItems.Count;

            Transform itemTransform = containable.GetGameObject().transform;

            // Check if a custom attachment point should be used
            Transform attachmentPoint = containable.AttachmentPoint;
            if (Mirrored && containable.AttachmentPointAlt != null)
            {
                attachmentPoint = containable.AttachmentPointAlt;
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

            displayedItems.Add(containable);
        }
        
        private void ContainerOnItemDetached(object sender, IContainable containable)
        {
            int index = displayedItems.FindIndex(x => x == containable);

            if (index == -1)
            {
                return;
            }

            Transform itemParent = containable.GetGameObject().transform.parent;
            if (itemParent != null && itemParent != containerDescriptor.displays[index])
            {
                containable.GetGameObject().transform.SetParent(null, true);
                Destroy(itemParent.gameObject);
            }

            displayedItems.Remove(containable);
        }
    }
}