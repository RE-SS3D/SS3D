using Coimbra;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
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

        /// <summary>Displays already warned about a missing calibration, so the warning shows once each.</summary>
        private readonly HashSet<Transform> _warnedUncalibrated = new();

        public void Awake()
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

            Transform display = attachedContainer.Displays[index].transform;
            PlaceAtDisplay(item, display, Mirrored, ReferenceRotation(display));
            _displayedItems[index] = item;
        }

        /// <summary>
        /// Parent <paramref name="item"/> to <paramref name="display"/> at its grip: the item's
        /// <c>AttachmentPoint</c> (or <c>AttachmentPointAlt</c> when <paramref name="mirrored"/>) sits on the
        /// display, turned by the display's calibrated reference rotation. Constant hand-local, so the same
        /// whatever pose the hand is in right now (spawn, mid-walk, Harm stance). Also used by the item pose
        /// contact sheet. Documents/plans/item-orientation.plan.md Phase A.
        /// </summary>
        public static void PlaceAtDisplay(Item item, Transform display, bool mirrored, Quaternion referenceRotation)
        {
            Transform itemTransform = item.transform;
            Transform attachmentPoint = item.AttachmentPoint;
            if (mirrored && item.AttachmentPointAlt != null)
            {
                attachmentPoint = item.AttachmentPointAlt;
            }

            if (attachmentPoint == null)
            {
                itemTransform.SetParent(display, false);
                itemTransform.localPosition = Vector3.zero;
                itemTransform.localRotation = Quaternion.identity;
                return;
            }

            GameObject temporaryPoint = new GameObject("TempPivotPoint");
            temporaryPoint.transform.SetParent(display, false);
            temporaryPoint.transform.localPosition = Vector3.zero;

            itemTransform.SetParent(temporaryPoint.transform, false);
            itemTransform.localRotation = Quaternion.identity;
            itemTransform.localPosition = -attachmentPoint.localPosition;
            temporaryPoint.transform.localRotation =
                HeldItemGrip.PivotLocalRotation(referenceRotation, attachmentPoint.localRotation);
        }

        private Quaternion ReferenceRotation(Transform display)
        {
            if (display.TryGetComponent(out GripSocket socket) && socket.IsCalibrated)
            {
                return socket.ReferenceRotation;
            }

            if (_warnedUncalibrated.Add(display))
            {
                Debug.LogWarning($"[ContainerItemDisplay] Display '{display.name}' on '{display.root.name}' has no calibrated "
                    + "GripSocket; held items use an identity reference and may sit at the wrong angle. "
                    + "Run SS3D → Inventory → Calibrate Hand Grip Sockets.", display);
            }

            return Quaternion.identity;
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

            Transform display = attachedContainer.Displays[index];
            Transform itemParent = item.transform.parent;
            if (itemParent != null && itemParent != display && itemParent.parent == display)
            {
                // Still on our grip pivot: let go of the item and drop the pivot.
                item.transform.SetParent(null, true);
                itemParent.gameObject.Dispose(true);
            }
            else
            {
                // The item already moved on (its new container's add arrived first on this client): it is
                // parented there now, so leave it and only clear away our empty pivots.
                for (int i = display.childCount - 1; i >= 0; i--)
                {
                    Transform child = display.GetChild(i);
                    if (child.name == "TempPivotPoint" && child.childCount == 0)
                    {
                        child.gameObject.Dispose(true);
                    }
                }
            }

            _displayedItems[index] = null;
        }
    }
}
