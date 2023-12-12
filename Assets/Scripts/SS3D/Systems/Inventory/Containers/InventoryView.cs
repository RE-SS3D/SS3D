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
using UnityEngine.UI;
using TMPro;
using FishNet.Object;
using FishNet;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// This handles displaying the container slots in the UI of the player.
    /// </summary>
    public class InventoryView : View
    {
        // A reference to the Inventory it displays
        private HumanInventory Inventory;

        [SerializeField] private GameObject HorizontalLayout;

        // Slots prefab in the horizontal layout
        [SerializeField] private GameObject PocketPrefab;
        [SerializeField] private GameObject IDSlotPrefab;
        [SerializeField] private GameObject HandLeftPrefab;
        [SerializeField] private GameObject HandRightPrefab;
        [SerializeField] private GameObject BagPrefab;
        [SerializeField] private GameObject BeltPrefab;
        [SerializeField] private GameObject Divisor;

        /// <summary>
        /// The order in which each containerType in the Horizontal layout should display.
        /// </summary>
        [SerializeField]
        private List<ContainerType> HorizontalSlotOrder;

        [SerializeField] private GameObject ClothingLayout;

        // Slots prefab for the Clothing grid.
        [SerializeField] private GameObject ShoeLeftPrefab;
        [SerializeField] private GameObject ShoeRightPrefab;
        [SerializeField] private GameObject GloveLeftPrefab;
        [SerializeField] private GameObject GloveRightPrefab;
        [SerializeField] private GameObject GlassesPrefab;
        [SerializeField] private GameObject MaskPrefab;
        [SerializeField] private GameObject HeadPrefab;
        [SerializeField] private GameObject JumpsuitPrefab;
        [SerializeField] private GameObject ExoSuitPrefab;
        [SerializeField] private GameObject EarLeftPrefab;
        [SerializeField] private GameObject EarRightPrefab;
        [SerializeField] private GameObject DummyPrefab;


        private object lockObject = new object();

        /// <summary>
        /// The order in which clothing slots appear in the grid by container type,
        /// from top left, to bottom right.
        /// </summary>
        [SerializeField]
        private List<ContainerType> ClothingSlotPosition;

        // All the slots present in the UI.
        private List<SingleItemContainerSlot> Slots = new();

        // The number of Hand slots.
        public int CountHandsSlots => Slots.Where(x => x.ContainerType == ContainerType.Hand).Count();
        
        private bool HasSlotOfType(ContainerType type)
        {
            return Slots.Where(x=>x.ContainerType == type).Count() > 0; 
        }

        private bool HasSlotWithHigherOrSameOrderThan(ContainerType type)
        {
            return Slots.Where(x => OrderOfType(x.ContainerType) >= OrderOfType(type)).Count() > 0;
        }

        public void Setup(HumanInventory inventory)
        {
            FillClothingLayoutWithDummySlots();
            Inventory = inventory;

            foreach(var container in inventory.Containers)
            {
                HandleInventoryContainerAdded(container);
            }

            inventory.OnInventoryContainerAdded += HandleInventoryContainerAdded;
            inventory.OnInventoryContainerRemoved += HandleInventoryContainerRemoved;
        }

        /// <summary>
        /// Necessary method to set up the clothing slot grid. It fills the whole grid with invisible dummy slots.
        /// </summary>
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

        /// <summary>
        /// Handle adding the right slot to the right place when a container is added to the inventory.
        /// </summary>
        [Client]
        private void HandleInventoryContainerAdded(AttachedContainer container)
        {
            if (Slots.Exists(x => x.Container == container))
                return;

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

                case ContainerType.Belt:
                    slot = AddHorizontalLayoutSlot(BeltPrefab, ContainerType.Belt);
                    break;


                default:
                    Log.Error(this, $"Unknown or missing container type {container.Type} for this container {container}");
                    slot = null;
                    break;
            }
            if (slot == null) return;
            slot.Container = container;
            slot.Inventory = Inventory;
            Slots.Add(slot);
        }

        /// <summary>
        /// For a given container type in the Horizontal slot, give it's order from left to right. 
        /// </summary>
        private int OrderOfType(ContainerType type) 
        {
            return HorizontalSlotOrder.FindIndex(0, x => x == type);
        }

        /// <summary>
        /// Hand slots need a special treatment, as there's left and right hand.
        /// </summary>
        private SingleItemContainerSlot AddHandSlot()
        {
            if (CountHandsSlots % 2 == 0)
            {
                return AddHorizontalLayoutSlot(HandLeftPrefab, ContainerType.Hand);
            }
            else return AddHorizontalLayoutSlot(HandRightPrefab, ContainerType.Hand);
        }

        /// <summary>
        /// This place a slot in the horizontal layout according to the order defined 
        /// by element order in the HorizontalSlotOrder List of containerType.
        /// In case of multiple slots with the same container type, it places the slot after all other slots
        /// with same container type.
        /// </summary>
        private int PlaceHorizontalLayoutSlot(ContainerType type)
        {
            //TODO fix hands again

            // if no slot with order higher place at end
            if (!HasSlotWithHigherOrSameOrderThan(type))
            {
                return HorizontalLayout.transform.childCount;   
            }

            // if slot with same order, place at last index of slot type.
            if (HasSlotOfType(type))
            {
                return (LastIndexSlotOfType(type)+1);
            }

            // if slot with order lower, place at last index of type just below.
            for (int i = 0; i < HorizontalLayout.transform.childCount; i++)
            {
                var childTransform = HorizontalLayout.transform.GetChild(i);
                
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && OrderOfType(slot.ContainerType) >= OrderOfType(type))
                {
                        return i;     
                }
            }
            Log.Warning(this, "returning slot position 0, should not reach this point");
            return 0;
        }

        /// <summary>
        /// Returns the game object sibling index of the ith game object being a 
        /// SingleContainerSlot and having a given ContainerType in the horizontal layout.
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
        /// Returns the game object sibling index of the last game object being a
        /// SingleContainerSlot and having a given ContainerType in the horizontal Layout.
        /// </summary>
        /// <returns> Index 0 if no slot with container type given is found, otherwise the last index slot of the given type.</returns>
        private int LastIndexSlotOfType(ContainerType type)
        {
            SingleItemContainerSlot slotOfType = null;
            for (int i = 0; i < HorizontalLayout.transform.childCount; i++)
            {
                
                var childTransform = HorizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.ContainerType == type)
                {
                    slotOfType = slot;
                }
            }
            if (slotOfType == null)
            {
                Log.Warning(this, "no slots of type " + type.ToString() + ", returning index 0 ");
                return 0;
            }
            else
            {
                return slotOfType.gameObject.transform.GetSiblingIndex();
            }
               
        }

        /// <summary>
        /// Add clothing slot, which are slots in the left/down part of the screen, organised in a grid.
        /// The order of apparition in the grid is from top left to bottom right, and is defined by the ClothingSlotPosition list.
        /// </summary>
        private SingleItemContainerSlot AddClothingSlot(GameObject prefabToInstantiate)
        {
            GameObject clothingSlot = Instantiate(prefabToInstantiate, transform);
            clothingSlot.transform.SetParent(ClothingLayout.transform, false);
            clothingSlot.gameObject.TryGetComponent(out SingleItemContainerSlot slot);
            int clothPosition = ClothingSlotPosition.FindIndex(0, x => x == slot.ContainerType);

            int slotPositionForContainerType = ClothingSlotPosition.FindIndex(0, x => x == slot.ContainerType);

            Transform currentSlotTransform = ClothingLayout.transform.GetChild(slotPositionForContainerType);

            // Remove the place holder dummy slot first.
            if (currentSlotTransform.gameObject.TryGetComponent(out DummySlot currentSlot))
            {
                currentSlotTransform.transform.SetParent(null, false);
                currentSlotTransform.gameObject.Dispose(true);
            };

            clothingSlot.transform.SetSiblingIndex(clothPosition);
            return slot;
        }

        /// <summary>
        /// Add slot in the horizontal layout, such as bag, hand, id ...
        /// </summary>
        private SingleItemContainerSlot AddHorizontalLayoutSlot(GameObject prefab, ContainerType type)
        {
            GameObject slot = Instantiate(prefab, transform);
            int slotIndex = PlaceHorizontalLayoutSlot(type);
            slot.transform.SetParent(HorizontalLayout.transform, false);
            slot.transform.SetSiblingIndex(slotIndex);
            return slot.GetComponent<SingleItemContainerSlot>(); 
        }

        /// <summary>
        /// Get the transform of a hand slot game object.
        /// </summary>
        /// <param name="index"> The index of the hand slot, necessary as multiple hand slots can be on a player. </param>
        /// <returns> The transform of the hand slot at the specified index.</returns>
        public Transform GetHandSlot(Hand hand)
        {
            SingleItemContainerSlot slot = Slots.Find(x => x.Container == hand.Container);
            return slot.transform;
        }

        /// <summary>
        /// Remove the corresponding slot when a container is removed from the inventory.
        /// Replace with an empty slot if it's a clothing type of container.
        /// </summary>
        private void HandleInventoryContainerRemoved(AttachedContainer container)
        {
            int indexToRemove = Slots.FindIndex(slot => slot.Container == container);
            
            // Replace the removed slot with a dummy slot if it's a clothing type of slot.
            // This allow the grid layout elements to keep their positions despite having a slot removed.
            if(ClothingSlotPosition.Contains(container.Type)){
                GameObject dummySlot = Instantiate(DummyPrefab);
                dummySlot.transform.SetParent(ClothingLayout.transform, false);
                int clothPosition = ClothingSlotPosition.FindIndex(0, x => x == Slots[indexToRemove].ContainerType);
                dummySlot.transform.SetSiblingIndex(clothPosition);
            }

            SingleItemContainerSlot slot = Slots[indexToRemove];
            if (slot == null) return;

            slot?.gameObject.Dispose(true);
            Slots.RemoveAt(indexToRemove);
        }

		public void DestroyAllSlots()
		{
			for(int i = Slots.Count-1; i>=0; i--)
			{
				Slots[i].gameObject.Dispose(true);
			}
		}
    }
}