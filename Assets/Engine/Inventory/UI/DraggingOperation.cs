using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Interactions.UI
{
    using static UIAbstractContainer;

    /**
     * Handles an interface dragging event, from the start of the drag through to the end.
     * Uses a UIInventoryHandler to perform all commands relating to moving the item.
     * 
     * When a hover begins, the item being dragged will be instantly moved into the holding slot (e.g. the hand), made slightly transparent, and when dragging ends, 
     * the item will be either moved to the given place if valid, or moved back to the original slot.
     */
    public class DraggingOperation : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public struct UISlotRef
        {
            public UISlotRef(UIAbstractContainer container, UIItemSlot slot)
            {
                this.container = container;
                this.slot = slot;
            }

            public UIAbstractContainer container;
            public UIItemSlot slot;
        }

        /**
         * Finds either the slot and container, or just container, or nothing, at the given point.
         */
        public static UISlotRef FindSlotAtPointer(PointerEventData eventData)
        {
            // Check if hovering
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // Find what we might be hovering over
            UIAbstractContainer foundContainer = null;
            UIItemSlot foundSlot = null;

            foreach (var result in results) {
                var container = result.gameObject.GetComponent<UIAbstractContainer>();
                if (container != null)
                    foundContainer = container;

                var slot = result.gameObject.GetComponent<UIItemSlot>();
                if (slot != null)
                    foundSlot = slot;
            }

            // TODO: This is a hack that will be removed soon
            if (foundSlot && !foundContainer) {
                Transform obj = foundSlot.transform.parent;
                while (obj) {
                    if (obj.GetComponent<UIAbstractContainer>()) {
                        foundContainer = obj.GetComponent<UIAbstractContainer>();
                        break;
                    }

                    obj = obj.parent;
                }
            }

            UISlotRef slotRef;
            slotRef.container = foundContainer;
            slotRef.slot = foundSlot;

            return slotRef;
        }

        public bool OnBeginDrag(SlotInfo origin, PointerEventData eventData)
        {
            this.origin = origin;

            if (origin.container.GetItem(origin.index) == null)
                return false;

            // Copy the sprite into a new object that will be dragged around
            draggingSprite = origin.uiSlot.CreateDraggableSprite(eventData.position, new Quaternion(), canvas.transform);

            holding = uiInventory.GetHoldingSlot();

            // Determine the slot the item is held in, whilst moving from origin to destination.
            // This slot will be the inventory's holdingSlot (if present), else we will just use the origin slot.
            if (holding.container == null) holding = origin;
            else {
                // Check whether we are dragging from the holding slot. If we are not, we move the item to the holding slot
                // and do some graphicalities.
                if (origin.container != holding.container || origin.index != holding.index) {

                    // If we can't move to the holding slot, we have to quit
                    if (!uiInventory.CanMoveItem(origin.container, origin.index, holding.container, holding.index))
                    {
                        Destroy(draggingSprite);
                        draggingSprite = null;
                        return false;
                    }

                    uiInventory.MoveItem(origin.container, origin.index, holding.container, holding.index);
                    holding.uiSlot.Transparent = true;
                }
            }

            eventData.pointerDrag = gameObject;

            return true;
        }
        public void OnDrag(PointerEventData eventData)
        {
            draggingSprite.transform.position = eventData.position;

            // Check if hovering
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // Find what we might be hovering over
            UISlotRef hover = FindSlotAtPointer(eventData);

            // If they are different, we end an old hover and start a new one
            if (prevHover.container != hover.container || prevHover.slot != hover.slot) {
                if (prevHover.slot && prevHoverSlot.container != null)
                    prevHover.slot.Highlighted = false;
                else if (prevHover.container && uiInventory.CanMoveItem(holding.container, holding.index, prevHover.container))
                    prevHover.container.Highlighted = false;

                prevHover = hover;

                if (hover.slot) {
                    var hoverSlot = hover.container.GetSlotLink(hover.slot);
                    if (hoverSlot.container != null && holding.container.GetItem(holding.index) != null && uiInventory.CanMoveItem(holding.container, holding.index, hoverSlot.container, hoverSlot.index)) {
                        prevHoverSlot = hoverSlot;
                        hover.slot.Highlighted = true;
                    }
                }
                else if (hover.container && uiInventory.CanMoveItem(holding.container, holding.index, hover.container))
                    hover.container.Highlighted = true;

            }
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            // Stop any hovering
            if (prevHover.slot)
                prevHover.slot.Highlighted = false;
            else if (prevHover.container)
                prevHover.container.Highlighted = false;

            holding.uiSlot.Transparent = false;

            // Revert the dragging object
            if (draggingSprite != null)
            {
                Destroy(draggingSprite);
                draggingSprite = null;
            }
            prevHover = new UISlotRef();
            prevHoverSlot = new SlotInfo();

            // Now attempt the actual item movement
            var destination = FindSlotAtPointer(eventData);

            bool moveOccured = false;

            if (destination.slot && destination.slot.enabled) {
                // Move from holding to destination
                var destinationSlot = destination.container.GetSlotLink(destination.slot);

                if (holding.container == destinationSlot.container && holding.index == destinationSlot.index)
                    moveOccured = true;
                else if (uiInventory.CanMoveItem(holding.container, holding.index, destinationSlot.container, destinationSlot.index)) {
                    uiInventory.MoveItem(holding.container, holding.index, destinationSlot.container, destinationSlot.index);
                    moveOccured = true;
                }
            }
            else if (destination.container) {
                if (uiInventory.FindHandler(holding.container) == destination.container)
                    moveOccured = true;
                else if (uiInventory.CanMoveItem(holding.container, holding.index, destination.container)) {
                    uiInventory.MoveItem(holding.container, holding.index, destination.container);
                    moveOccured = true;
                }
            }

            // If we could not move it to the intended place, move the item back to the original slot.
            if (moveOccured == false && uiInventory.CanMoveItem(holding.container, holding.index, origin.container, origin.index))
                uiInventory.MoveItem(holding.container, holding.index, origin.container, origin.index);

            origin = new SlotInfo();
            holding = new SlotInfo();

            Destroy(this);
        }

        public GameObject canvas;
        public UIInventoryHandler uiInventory;

        private SlotInfo origin;
        private SlotInfo holding;

        private GameObject draggingSprite;

        private UISlotRef prevHover;
        private SlotInfo prevHoverSlot;
    }
}