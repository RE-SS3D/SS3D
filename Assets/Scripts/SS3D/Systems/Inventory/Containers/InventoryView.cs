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

namespace SS3D.Systems.Inventory.UI
{
    public class InventoryView : View
    {

        public Containers.Inventory Inventory;
        public GameObject PocketPrefab;
        public GameObject IDSlotPrefab;
        public GameObject HandLeftPrefab;
        public GameObject HandRightPrefab;
        public GameObject Divisor;

        public GameObject HorizontalLayout;

        public List<GameObject> Slots = new();

        // Maybe HandsUI should only handle selected hand highlight and inventory UI
        // should handle setting up containers to UI.
        public void Setup(Containers.Inventory inventory)
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

                default:
                    Punpun.Error(this, "Unknown or missing container type for this container");
                    slot = null;
                    break;
            }
            slot.Container = container;
            slot.Inventory = Inventory;
        }

        private SingleItemContainerSlot AddHandSlot()
        {
            GameObject hand;
            if (Inventory.CountHands % 2 == 0)
            {
                hand = Instantiate(HandRightPrefab, transform);
            }
            else hand = Instantiate(HandLeftPrefab, transform);

            hand.transform.parent = HorizontalLayout.transform;

            // Put the hand containers just before the pocket containers.
            for(int i=0; i< HorizontalLayout.transform.childCount; i++)
            {
                var childTransform = HorizontalLayout.transform.GetChild(i);
                if(childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.ContainerType == ContainerType.Pocket)
                {
                    hand.transform.SetSiblingIndex(i);
                    break;
                }
            }

            return hand.GetComponent<SingleItemContainerSlot>();
        }

        
        private SingleItemContainerSlot AddPocketSlot()
        {
            GameObject pocket = Instantiate(PocketPrefab, transform);
            pocket.transform.parent = HorizontalLayout.transform;
            // Pocket go  to the far right of the UI.
            pocket.transform.SetAsLastSibling();
            return pocket.GetComponent<SingleItemContainerSlot>();
        }

        private SingleItemContainerSlot AddIdentificationSlot()
        {
            GameObject ID = Instantiate(IDSlotPrefab, transform);
            ID.transform.parent = HorizontalLayout.transform;
            // ID slots go  to the far left of the UI.
            ID.transform.SetAsFirstSibling();
            return ID.GetComponent<SingleItemContainerSlot>();
        }

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
            return HorizontalLayout.transform.GetChild(index+childIndex);
        }


        void OnInventoryContainerRemoved(AttachedContainer container)
        {

        }
    }
}
