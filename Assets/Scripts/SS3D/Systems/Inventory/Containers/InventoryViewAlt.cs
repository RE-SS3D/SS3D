
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.UI
{
    public class InventoryViewAlt : View
    {

        public InventoryAlt Inventory;
        public GameObject PocketPrefab;
        public GameObject IDSlotPrefab;
        public GameObject HandLeftPrefab;
        public GameObject HandRightPrefab;

        public GameObject HorizontalLayout;

        public List<GameObject> Slots = new();

        // Maybe HandsUI should only handle selected hand highlight and inventory UI
        // should handle setting up containers to UI.
        public void Setup(InventoryAlt inventory)
        {
            Inventory = inventory;
            inventory.OnInventoryContainerAdded += OnInventoryContainerAdded;
            inventory.OnInventoryContainerRemoved += OnInventoryContainerRemoved;

        }

        void OnInventoryContainerAdded(AttachedContainer container)
        {
            switch(container.Type)
            {
                case ContainerType.Hand:
                    AddHandSlot();
                    break;

                case ContainerType.Pocket:
                    AddPocketSlot();
                    break;

                case ContainerType.Identification:
                    AddIdentificationSlot();
                    break;

            }

        }

        private void AddHandSlot()
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
                if(childTransform.gameObject.GetComponent<SingleItemContainerSlot>().ContainerType == ContainerType.Pocket)
                {
                    hand.transform.SetSiblingIndex(i);
                    break;
                }
            }
        }

        
        private void AddPocketSlot()
        {
            GameObject pocket = Instantiate(PocketPrefab, transform);
            pocket.transform.parent = HorizontalLayout.transform;
            // Pocket go  to the far right of the UI.
            pocket.transform.SetAsLastSibling();
        }

        private void AddIdentificationSlot()
        {
            GameObject ID = Instantiate(IDSlotPrefab, transform);
            ID.transform.parent = HorizontalLayout.transform;
            // ID slots go  to the far left of the UI.
            ID.transform.SetAsFirstSibling();
        }


        void OnInventoryContainerRemoved(AttachedContainer container)
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
