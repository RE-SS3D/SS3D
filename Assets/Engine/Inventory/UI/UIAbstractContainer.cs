using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SS3D.Engine.Inventory;

namespace SS3D.Engine.Interactions.UI
{

    /**
     * Handles all common stuff for rendering container/s on the UI
     */
    public abstract class UIAbstractContainer : MonoBehaviour, UIItemSlot.SlotInteractor
    {
        #region Types
        /**
         * The inventory handler performs all the responsibilities of actually performing actions on the
         * player based on the UI.
         */
        public interface UIInventoryHandler
        {
            SlotInfo GetHoldingSlot();
            UIAbstractContainer FindHandler(Container container);

            void OnDragStart(SlotInfo slot, PointerEventData eventData);

            bool CanMoveItem(Container from, int fromSlot, Container to, int toSlot);
            bool CanMoveItem(Container from, int fromSlot, UIAbstractContainer to); // TODO: Move into UIAbstractContainer

            void MoveItem(Container from, int fromSlot, Container to, int toSlot);
            void MoveItem(Container from, int fromSlot, UIAbstractContainer to); // TODO: Move into UIAbstractContainer
            void MoveSelectedItem(Container container, int slot);

            void DropItem(Container from, int fromSlot, Vector2 screenPosition);
        }
        /**
         * Internally used for describing a ui slot
         */
        public struct SlotInfo
        {
            public SlotInfo(Container container, int index, UIItemSlot uiSlot)
            {
                this.container = container;
                this.index = index;
                this.uiSlot = uiSlot;
            }

            public Container container;
            public int index;
            public UIItemSlot uiSlot;
        }
        #endregion

        public abstract bool Highlighted { get; set; }

        [NonSerialized]
        public UIInventoryHandler inventoryHandler;

        public void UpdateContainers(GameObject newOwner, List<Container> newContainers)
        {
            var oldContainers = containers;

            RenderContainers(newOwner, newContainers);

            /* C# events are very silly.
             * To be able to add AND remove delegates (which we need to do), we cannot allow the delegates
             * to be anonymous, because if they are there is no way of referencing them. Therefore we have to create
             * the delegate, store it, and re-use that reference to remove the delegate.
             * 
             * IF at any point in the chain an anonymous function is used, the whole thing goes kaput.
             */
            foreach (var container in oldContainers) {
                if (!newContainers.Contains(container)) {
                    container.onChange -= delegates[container];
                    delegates.Remove(container);
                }
            }
            foreach (var container in newContainers) {
                if (!oldContainers.Contains(container)) {
                    Mirror.SyncList<GameObject>.SyncListChanged del = (a, b, c, d) => RenderContainer(container);
                    delegates.Add(container, del);
                    container.onChange += del;
                }
            }

            owner = newOwner;
            if (containers == oldContainers)
                containers = newContainers;
        }
        public virtual void Unlink()
        {
            foreach (var container in containers)
                container.onChange -= delegates[container];
            delegates.Clear();
        }

        /**
         * Get the container slot referred to by a UIItemSlot in this renderer's posession
         */
        public SlotInfo GetSlotLink(UIItemSlot slot)
        {
            return slots.Find(slotInfo => slotInfo.uiSlot == slot);
        }
        public UIItemSlot GetUISlot(Container container, int slotIndex)
        {
            return slots.Find(slotInfo => slotInfo.container == container && slotInfo.index == slotIndex).uiSlot;
        }

        // The object that owns all of the containers being rendered. TODO: Is this used?
        [NonSerialized]
        public GameObject owner;
        // All containers being rendered
        [NonSerialized]
        public List<Container> containers = new List<Container>();

        /**
         * Called whenever the list of containers and/or their owner changes.
         * Note: This method can update container list. If it does not, list will be updated for it
         */
        protected abstract void RenderContainers(GameObject newOwner, List<Container> newContainers);
        /**
         * Called whenever the contents of the given container changes
         */
        protected abstract void RenderContainer(Container container);

        // Implement interfaces
        public virtual void OnPress(UIItemSlot slot, PointerEventData.InputButton button)
        {
            var info = GetSlotLink(slot);

            if (button == PointerEventData.InputButton.Left && info.container != null)
                inventoryHandler.MoveSelectedItem(info.container, info.index);
        }

        void UIItemSlot.SlotInteractor.StartHover(UIItemSlot hovering, UIAbstractContainer overContainer, UIItemSlot over)
        {
            // TODO: Check if compatible, if not, do something other than highlighting.
            if (over)
                over.Highlighted = true;
            else
                overContainer.Highlighted = true;
        }
        void UIItemSlot.SlotInteractor.StopHover(UIItemSlot hovering, UIAbstractContainer overContainer, UIItemSlot over)
        {
            // TODO: If not compatible, do something other than un-highlighting
            if (over)
                over.Highlighted = false;
            else
                overContainer.Highlighted = false;
        }
        void UIItemSlot.SlotInteractor.OnDragStart(UIItemSlot from, PointerEventData eventData)
        {
            inventoryHandler.OnDragStart(GetSlotLink(from), eventData);
        }

        protected List<SlotInfo> slots = new List<SlotInfo>();

        // Used for saving the delegate information, as C#'s delegate event handling is unfortunately inefficient
        private Dictionary<Container, Mirror.SyncList<GameObject>.SyncListChanged> delegates = new Dictionary<Container, Mirror.SyncList<GameObject>.SyncListChanged>();
    }
}