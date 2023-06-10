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
using TMPro;
using FishNet.Object;
using FishNet;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// This handles displaying, removing, adding the container slots in the inventory of the player.
    /// </summary>
    public class InventoryView : View
    {

        private HumanInventory Inventory;

        // Slots in the horizontal layout
        public GameObject HorizontalLayout;
        public GameObject PocketPrefab;
        public GameObject IDSlotPrefab;
        public GameObject HandLeftPrefab;
        public GameObject HandRightPrefab;
        public GameObject BagPrefab;
        public GameObject Divisor;

        [SerializeField]
        private List<ContainerType> HorizontalSlotOrder;


        public GameObject ClothingLayout;
        public GameObject ShoeLeftPrefab;
        public GameObject ShoeRightPrefab;
        public GameObject GloveLeftPrefab;
        public GameObject GloveRightPrefab;
        public GameObject GlassesPrefab;
        public GameObject MaskPrefab;
        public GameObject HeadPrefab;
        public GameObject JumpsuitPrefab;
        public GameObject ExoSuitPrefab;
        public GameObject DummyPrefab;
        public GameObject EarLeftPrefab;
        public GameObject EarRightPrefab;


        [SerializeField]
        private List<ContainerType> ClothingSlotPosition;

        private List<SingleItemContainerSlot> Slots = new();

        public int CountHandsSlots => Slots.Where(x => x.ContainerType == ContainerType.Hand).Count();

        public void Setup(HumanInventory inventory)
        {
            FillClothingLayoutWithDummySlots();
            Inventory = inventory;
            inventory.OnInventoryContainerAdded += OnInventoryContainerAdded;
            inventory.OnInventoryContainerRemoved += OnInventoryContainerRemoved;
            

        }

        [Client]
        private void FillClothingLayoutWithDummySlots()
        {
            for(int i=0; i< ClothingSlotPosition.Count; i++)
            {
                var dummySlot = Instantiate(DummyPrefab);
                dummySlot.transform.SetParent(ClothingLayout.transform, false);
                dummySlot.transform.SetAsFirstSibling();
            }
        }

        [Client]
        void OnInventoryContainerAdded(AttachedContainer container)
        {
            SingleItemContainerSlot slot;
            switch (container.Type)
            {
                case ContainerType.Hand:
                    slot = AddHandSlot();
                    break;

                case ContainerType.Pocket:
                    slot = AddHorizontalLayoutSlot(PocketPrefab, ContainerType.Pocket);
                    break;

                case ContainerType.Identification:
                    slot = AddHorizontalLayoutSlot(IDSlotPrefab, ContainerType.Identification);
                    break;

                case ContainerType.Bag:
                    slot = AddHorizontalLayoutSlot(BagPrefab, ContainerType.Bag);
                    break;

                case ContainerType.Glasses:
                    slot = AddClothingSlot(GlassesPrefab);
                    break;

                case ContainerType.Mask:
                    slot = AddClothingSlot(MaskPrefab);
                    break;

                case ContainerType.EarLeft:
                    slot = AddClothingSlot(EarLeftPrefab);
                    break;

                case ContainerType.EarRight:
                    slot = AddClothingSlot(EarRightPrefab);
                    break;

                case ContainerType.Head:
                    slot = AddClothingSlot(HeadPrefab);
                    break;

                case ContainerType.ExoSuit:
                    slot = AddClothingSlot(ExoSuitPrefab);
                    break;

                case ContainerType.Jumpsuit:
                    slot = AddClothingSlot(JumpsuitPrefab);
                    break;

                case ContainerType.GloveLeft:
                    slot = AddClothingSlot(GloveLeftPrefab);
                    break;

                case ContainerType.GloveRight:
                    slot = AddClothingSlot(GloveRightPrefab);
                    break;

                case ContainerType.ShoeLeft:
                    slot = AddClothingSlot(ShoeLeftPrefab);
                    break;

                case ContainerType.ShoeRight:
                    slot = AddClothingSlot(ShoeRightPrefab);
                    break;

                default:
                    Punpun.Error(this, $"Unknown or missing container type {container.Type} for this container {container}");
                    slot = null;
                    break;
            }
            if (slot == null) return;
            slot.Container = container;
            slot.Inventory = Inventory;
            Slots.Add(slot);
        }

        private int OrderOfType(ContainerType type) 
        {
            return HorizontalSlotOrder.FindIndex(0, x => x == type);
        }

        private SingleItemContainerSlot AddHandSlot()
        {
            if (CountHandsSlots % 2 == 0)
            {
                return AddHorizontalLayoutSlot(HandRightPrefab, ContainerType.Hand);
            }
            else return AddHorizontalLayoutSlot(HandLeftPrefab, ContainerType.Hand);
        }

        /// <summary>
        /// This place a slot in the horizontal layout according to the order defined by element order in the HorizontalSlotOrder List of containerType.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private int PlaceSlot(ContainerType type)
        {
            for (int i = 0; i < HorizontalLayout.transform.childCount; i++)
            {
                var childTransform = HorizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && OrderOfType(slot.ContainerType) >= OrderOfType(type))
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns the game object sibling index of the ith game object being a SingleContainerSlot and having a given ContainerType in the horizontal layout.
        /// </summary>
        /// <returns> Index 0 if no slot with container type given is found, otherwise the ith index slot of the given type.</returns>
        private int IndexSlotOfType(ContainerType type, int number)
        {
            int countOfGivenType = 0;
            for (int i = 0; i < HorizontalLayout.transform.childCount; i++)
            {
                var childTransform = HorizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.ContainerType == type)
                {
                    if(number == countOfGivenType)
                    {
                        return i;
                    }
                    countOfGivenType++;   
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns the game object sibling index of the last game object being a SingleContainerSlot and having a given ContainerType in the horizontal Layout.
        /// </summary>
        /// <returns> Index 0 if no slot with container type given is found, otherwise the last index slot of the given type.</returns>
        private int LastIndexSlotOfType(ContainerType type, int number)
        {
            var slotsOfType = new List<SingleItemContainerSlot>();
            for (int i = 0; i < HorizontalLayout.transform.childCount; i++)
            {
                var childTransform = HorizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.ContainerType == type)
                {
                    slotsOfType.Add(slot);
                }
            }
            if (slotsOfType.Count == 0)
                return 0;
            else
                return slotsOfType.IndexOf(slotsOfType.Last());
        }

        private SingleItemContainerSlot AddClothingSlot(GameObject prefabToInstantiate)
        {
            GameObject clothingSlot = Instantiate(prefabToInstantiate, transform);
            clothingSlot.transform.SetParent(ClothingLayout.transform, false);
            clothingSlot.gameObject.TryGetComponent(out SingleItemContainerSlot slot);
            int clothPosition = ClothingSlotPosition.FindIndex(0, x => x == slot.ContainerType);
            var currentSlot = ClothingLayout.transform.GetChild(ClothingSlotPosition.FindIndex(0, x => x == slot.ContainerType));
            clothingSlot.transform.SetSiblingIndex(clothPosition);
            currentSlot.transform.SetParent(null, false);
            currentSlot.gameObject.Dispose(true);
            return slot;
        }

        private SingleItemContainerSlot AddHorizontalLayoutSlot(GameObject prefab, ContainerType type)
        {
            GameObject slot = Instantiate(prefab, transform);
            slot.transform.SetParent(HorizontalLayout.transform, false);
            // Pocket go  to the far right of the UI.
            slot.transform.SetSiblingIndex(PlaceSlot(type));
            return slot.GetComponent<SingleItemContainerSlot>();
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
            int indexToRemove = Slots.FindIndex(slot => slot.Container == container);
            Slots[indexToRemove].gameObject.Dispose(true);
            Slots.RemoveAt(indexToRemove);
        }
    }
}