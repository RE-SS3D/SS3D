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

        /// <summary>
        /// Grips set before the character's Animator has posed the hand bone. The pivot is a child of
        /// the hand, so a grip set against the bind pose (a loadout item at spawn) stays wrong until
        /// the item is picked up again (#158). Re-set once the hand has been animated.
        /// </summary>
        private readonly List<PendingGrip> _pendingGrips = new();

        /// <summary>Re-set the grip anyway after this many frames, so an Animator that never starts can't leak entries.</summary>
        private const int MaxGripWaitFrames = 60;

        private readonly struct PendingGrip
        {
            public readonly Transform Pivot;
            public readonly Transform CharacterRoot;
            public readonly Quaternion AttachmentRotation;
            public readonly Animator Animator;
            public readonly int AttachedFrame;

            public PendingGrip(Transform pivot, Transform characterRoot, Quaternion attachmentRotation, Animator animator, int attachedFrame)
            {
                Pivot = pivot;
                CharacterRoot = characterRoot;
                AttachmentRotation = attachmentRotation;
                Animator = animator;
                AttachedFrame = attachedFrame;
            }
        }

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

            Transform itemTransform = item.transform;

            // Check if a custom attachment point should be used
            Transform attachmentPoint = item.AttachmentPoint;
            if (Mirrored && item.AttachmentPointAlt != null)
            {
                attachmentPoint = item.AttachmentPointAlt;
            }

            if (attachmentPoint != null)
            {
                // #1226 used attachmentPoint.root (the ITEM) * localRotation — mid-air tumble became the grip.
                // Absolute world (identity * att) freezes world axes so hand-local drifts with facing.
                // Use the hand display's character root: hold ≈ character.facing * Attachment (stable).
                GameObject temporaryPoint = new GameObject("TempPivotPoint");
                Transform display = attachedContainer.Displays[index].transform;
                Transform characterRoot = display.root;

                temporaryPoint.transform.SetParent(display, false);
                temporaryPoint.transform.localPosition = Vector3.zero;

                itemTransform.SetParent(temporaryPoint.transform, false);
                temporaryPoint.transform.rotation = characterRoot.rotation * attachmentPoint.localRotation;
                itemTransform.localRotation = Quaternion.identity;
                itemTransform.localPosition = -attachmentPoint.localPosition;

                _pendingGrips.Add(new PendingGrip(
                    temporaryPoint.transform,
                    characterRoot,
                    attachmentPoint.localRotation,
                    display.GetComponentInParent<Animator>(),
                    Time.frameCount));
            }
            else
            {
                itemTransform.SetParent(attachedContainer.Displays[index].transform, false);
                itemTransform.localPosition = Vector3.zero;
                itemTransform.localRotation = Quaternion.identity;
            }

            _displayedItems[index] = item;
        }

        private void LateUpdate()
        {
            for (int i = _pendingGrips.Count - 1; i >= 0; i--)
            {
                PendingGrip grip = _pendingGrips[i];
                if (grip.Pivot == null || grip.CharacterRoot == null)
                {
                    _pendingGrips.RemoveAt(i);
                    continue;
                }

                int waited = Time.frameCount - grip.AttachedFrame;
                bool handAnimated = grip.Animator == null
                    || (grip.Animator.isActiveAndEnabled && grip.Animator.isInitialized);

                // LateUpdate runs after the Animator, so from the frame after attach the hand bone
                // holds a real animated pose rather than the bind pose.
                if (waited < 1 || (!handAnimated && waited < MaxGripWaitFrames))
                {
                    continue;
                }

                grip.Pivot.rotation = grip.CharacterRoot.rotation * grip.AttachmentRotation;
                _pendingGrips.RemoveAt(i);
            }
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
