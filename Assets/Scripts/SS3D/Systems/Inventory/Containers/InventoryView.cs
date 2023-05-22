using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using SS3D.Logging;
using UnityEngine.XR;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// This handles displaying, removing, adding the container slots in the inventory of the player.
    /// </summary>
    public class InventoryView : View
    {

        public HumanInventory Inventory;
        public GameObject PocketPrefab;
        public GameObject IDSlotPrefab;
        public GameObject HandLeftPrefab;
        public GameObject HandRightPrefab;
        public GameObject BagPrefab;
        public GameObject Divisor;

        public GameObject HorizontalLayout;

        public List<SingleItemContainerSlot> Slots = new();

        public int CountHandsSlots => Slots.Where(x => x.ContainerType == ContainerType.Hand).Count();


        // Maybe HandsUI should only handle selected hand highlight and inventory UI
        // should handle setting up containers to UI.
        public void Setup(HumanInventory inventory)
        {
            Inventory = inventory;
            inventory.OnInventoryContainerAdded += OnInventoryContainerAdded;
            inventory.OnInventoryContainerRemoved += OnInventoryContainerRemoved;
            var divisor = Instantiate(Divisor, transform);
            divisor.transform.parent = HorizontalLayout.transform;
            divisor.transform.SetAsFirstSibling();
        }

        void OnInventoryContainerAdded(AttachedContainer container)
        {
            SingleItemContainerSlot slot;
            switch (container.Type)
            {
                case ContainerType.Hand:
                    slot = AddHandSlot();
                    break;

                case ContainerType.Pocket:
                    slot = AddPocketSlot();
                    break;

                case ContainerType.Identification:
                    slot = AddIdentificationSlot();
                    break;

                case ContainerType.Bag:
                    slot = AddBagSlot();
                    break;

                default:
                    Punpun.Error(this, "Unknown or missing container type for this container");
                    slot = null;
                    break;
            }
            if (slot == null) return;
            slot.Container = container;
            slot.Inventory = Inventory;
            Slots.Add(slot);
        }

        private SingleItemContainerSlot AddHandSlot()
        {
            GameObject hand;
            if (CountHandsSlots % 2 == 0)
            {
                hand = Instantiate(HandRightPrefab, transform);
            }
            else hand = Instantiate(HandLeftPrefab, transform);

            hand.transform.parent = HorizontalLayout.transform;

            // Put the hand containers just before the pocket slots.
            hand.transform.SetSiblingIndex(FirstIndexSlotOfType(ContainerType.Pocket));

            return hand.GetComponent<SingleItemContainerSlot>();
        }

        /// <summary>
        /// Returns the game object sibling index of the first game object being a SingleContainerSlot and having a given ContainerType.
        /// </summary>
        /// <returns> Index 0 if no slot with container type given is found, otherwise the first index slot of the given type.</returns>
        private int FirstIndexSlotOfType(ContainerType type)
        {
            for (int i = 0; i < HorizontalLayout.transform.childCount; i++)
            {
                var childTransform = HorizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.ContainerType == type)
                {
                    return i;
                }
            }
            return 0;
        }

        
        private SingleItemContainerSlot AddPocketSlot()
        {
            GameObject pocket = Instantiate(PocketPrefab, transform);
            pocket.transform.parent = HorizontalLayout.transform;
            // Pocket go  to the far right of the UI.
            pocket.transform.SetAsLastSibling();
            return pocket.GetComponent<SingleItemContainerSlot>();
        }

        private SingleItemContainerSlot AddBagSlot()
        {
            GameObject bag = Instantiate(BagPrefab, transform);
            bag.transform.parent = HorizontalLayout.transform;
            // Put the bag slot just before the pocket slots.
            bag.transform.SetSiblingIndex(FirstIndexSlotOfType(ContainerType.Hand));

            return bag.GetComponent<SingleItemContainerSlot>();
        }


        private SingleItemContainerSlot AddIdentificationSlot()
        {
            GameObject ID = Instantiate(IDSlotPrefab, transform);
            ID.transform.parent = HorizontalLayout.transform;
            // ID slots go  to the far left of the UI.
            ID.transform.SetAsFirstSibling();
            return ID.GetComponent<SingleItemContainerSlot>();
        }

        /// <summary>
        /// Get the transform of a hand slot game object.
        /// </summary>
        /// <param name="index"> The index of the hand slot, necessary as multiple hand slots can be on a player. </param>
        /// <returns> The transform of the hand slot at the specified index.</returns>
        public Transform GetHandSlot(int index)
        {
            int childIndex = 0;
            for (int i = 0; i < HorizontalLayout.transform.childCount; i++)
            {
                var childTransform = HorizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.ContainerType == ContainerType.Hand)
                {
                    break;
                }
                childIndex++;
            }

            if(index+childIndex >= HorizontalLayout.transform.childCount)
            {
                Punpun.Warning(this, "index out of bound, check that the number of hand slots is greater than index.");
                return null;
            }

            return HorizontalLayout.transform.GetChild(index+childIndex);
        }


        void OnInventoryContainerRemoved(AttachedContainer container)
        {

        }
    }
}
