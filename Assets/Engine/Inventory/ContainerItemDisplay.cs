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
    public class ContainerItemDisplay : MonoBehaviour
    {
        /// <summary>
        /// The list of transforms defining where the items are displayed.
        /// </summary>
        public Transform[] Displays;

        public AttachedContainer Container;
        public bool Mirrored;

        /// <summary>
        /// The list of items displayed in the container;
        /// </summary>
        private List<IContainerizable> displayedItems;

        public void Start()
        {
            Assert.IsNotNull(Container);
            
            displayedItems = new List<IContainerizable>();
            Container.ItemAttached += ContainerOnItemAttached;
            Container.ItemDetached += ContainerOnItemDetached;
        }

        public void OnDestroy()
        {
            Container.ItemAttached -= ContainerOnItemAttached;
        }

        private void ContainerOnItemAttached(object sender, IContainerizable item)
        {
            // Defines the transform of the item to be the first available position.
            int index = displayedItems.Count;

            Transform itemTransform = item.GetGameObject().transform;

            // Check if a custom attachment point should be used
            Transform attachmentPoint = item.AttachmentPoint;
            if (Mirrored && item.AttachmentPointAlt != null)
            {
                attachmentPoint = item.AttachmentPointAlt;
            }

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

            displayedItems.Add(item);
        }
        
        private void ContainerOnItemDetached(object sender, IContainerizable item)
        {
            int index = displayedItems.FindIndex(x => x == item);

            if (index == -1)
            {
                return;
            }

            Transform itemParent = item.GetGameObject().transform.parent;
            if (itemParent != null && itemParent != Displays[index])
            {
                item.GetGameObject().transform.SetParent(null, true);
                Destroy(itemParent.gameObject);
            }

            displayedItems.Remove(item);
        }
    }
}