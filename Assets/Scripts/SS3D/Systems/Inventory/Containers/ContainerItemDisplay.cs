using Coimbra;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Systems.Inventory.Containers
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
        public AttachedContainer attachedContainer;
        public bool Mirrored;

        /// <summary>
        /// The list of items displayed in the container;
        /// </summary>
        private Item[] _displayedItems;

        public void Start()
        {
            Assert.IsNotNull(attachedContainer);
            
            _displayedItems = new Item[attachedContainer.Displays.Length];
            attachedContainer.OnItemAttached += ContainerOnItemAttached;
            attachedContainer.OnItemDetached += ContainerOnItemDetached;
        }

        public void OnDestroy()
        {
            attachedContainer.OnItemAttached -= ContainerOnItemAttached;
            attachedContainer.OnItemDetached -= ContainerOnItemDetached;
        }

        private void ContainerOnItemAttached(object sender, Item item)
        {
            // Defines the transform of the item to be the first available position.
            int index = -1;
            for (int i = 0; i < attachedContainer.Displays.Length; i++)
            {
                if (_displayedItems[i] == null)
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

                temporaryPoint.transform.SetParent(attachedContainer.Displays[index].transform, false);
                temporaryPoint.transform.localPosition = Vector3.zero;

                // Assign parent
                itemTransform.SetParent(temporaryPoint.transform, false);
                temporaryPoint.transform.rotation = attachmentPoint.root.rotation * attachmentPoint.localRotation;
               
                // Assign the relative position between the attachment point and the object
                itemTransform.localPosition = -attachmentPoint.localPosition;
                itemTransform.localRotation = Quaternion.identity;
            }
            else
            {
                itemTransform.SetParent(attachedContainer.Displays[index].transform, false);
                itemTransform.localPosition = new Vector3();
                itemTransform.localRotation = new Quaternion();
            }

            _displayedItems[index] = item;
        }

        private void ContainerOnItemDetached(object sender, Item item)
        {
            int index = -1;
            for (int i = 0; i < attachedContainer.Displays.Length; i++)
            {
                if (_displayedItems[i] == item)
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
            if (itemParent != null && itemParent != attachedContainer.Displays[index])
            {
                item.transform.SetParent(null, true);
                // It's currently deleting the game object containing the item, why is this here ?
                itemParent.gameObject.Dispose(true);
            }

            _displayedItems[index] = null;
        }
    }
}