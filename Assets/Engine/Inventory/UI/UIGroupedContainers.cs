using SS3D.Engine.Interactions.UI;
using System;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Inventory.UI
{
    /**
     * A component for rendering multiple seperate containers together as one group.
     * Will complain if there aren't enough item slots for a given container type.
     */
    public class UIGroupedContainers : UIAbstractContainer
    {
        // Matches a container type (expected to hold 1 item) to a slot
        [Serializable]
        public struct ItemSlot
        {
            // Type of slot this UI view connects to
            public Filter filter;
            // The UI view to display in
            public UIItemSlot slot;
        }

        // Editor properties
        [SerializeField] private ItemSlot[] uiSlots = null;

        // Runtime
        public override bool Highlighted { get; set; }

        public void Start()
        {
            foreach(var uiSlot in uiSlots)
                uiSlot.slot.slotInteractor = this;
        }

        protected override void RenderContainers(GameObject newOwner, List<Container> newContainers)
        {
            slots.Clear();

            bool[] isSet = new bool[uiSlots.Length];

            foreach (var container in newContainers)
            {
                for (int i = 0; i < container.Length(); ++i)
                {
                    var slotFilter = container.GetFilter(i);

                    // Get the first available slot to place the item info in
                    int slotIndex = Array.FindIndex(uiSlots, slot => slot.filter == slotFilter);
                    while (slotIndex != -1 && isSet[slotIndex])
                        slotIndex = Array.FindIndex(uiSlots, slotIndex + 1, slot => slot.filter == slotFilter);

                    if (slotIndex == -1)
                    {
                        // TODO: Perform this warning only when actually useful
                        // Debug.LogWarning("Nowhere to display slottype " + slotType.ToString() + " from " + container.containerName + " from " + owner.name + " in Grouped Container " + name);
                        continue;
                    }

                    isSet[slotIndex] = true;

                    slots.Add(new SlotInfo(container, i, uiSlots[slotIndex].slot));
                    uiSlots[slotIndex].slot.Item = container.GetItem(i);
                }
            }

            // Disable all unused slots
            for (int i = 0; i < uiSlots.Length; ++i)
                uiSlots[i].slot.enabled = isSet[i];
        }

        protected override void RenderContainer(Container container)
        {
            // As we dont ever have to instantiate objects, it should be fast enough to just do a complete rerender
            RenderContainers(owner, containers);
        }

        public override void OnPress(UIItemSlot slot, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Middle)
            {
                return;
            }
            
            SlotInfo info = GetSlotLink(slot);
            Filter slotFilter = info.container.GetFilter(info.index);
            // TODO: Hack, this should be decoupled in some way
            GameObject playerObject = info.container.gameObject;
            GameObject item = info.container.GetItem(info.index)?.gameObject;
            InteractionHandler handler = playerObject.GetComponent<InteractionHandler>();
            Hands hands = playerObject.GetComponent<Hands>();

            if (handler != null && hands != null && hands.GetActiveTool() != null && item != null && (slotFilter.Hash == Filters.RightHand || slotFilter.Hash == Filters.LeftHand))
            {
                handler.InteractInHand(item, playerObject, button == PointerEventData.InputButton.Right);
                return;
            }

            base.OnPress(slot, button);
        }

        // Used to store delegate used for performance reasons
        private readonly Container.ItemList.SyncListChanged OnContainerChange;
    }
}
