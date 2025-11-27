using Coimbra;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// This handles displaying the container slots in the UI of the player.
    /// </summary>
    public class InventoryView : View
    {
        // All the slots present in the UI.
        private readonly List<SingleItemContainerSlot> _slots = new();

        // A reference to the Inventory it displays
        private HumanInventory _inventory;

        [FormerlySerializedAs("HorizontalLayout")]
        [SerializeField]
        private GameObject _horizontalLayout;

        // Slots prefab in the horizontal layout
        [FormerlySerializedAs("PocketPrefab")]
        [SerializeField]
        private GameObject _pocketPrefab;

        [FormerlySerializedAs("IDSlotPrefab")]
        [SerializeField]
        private GameObject _idSlotPrefab;

        [FormerlySerializedAs("HandLeftPrefab")]
        [SerializeField]
        private GameObject _handLeftPrefab;

        [FormerlySerializedAs("HandRightPrefab")]
        [SerializeField]
        private GameObject _handRightPrefab;

        [FormerlySerializedAs("BagPrefab")]
        [SerializeField]
        private GameObject _bagPrefab;

        [FormerlySerializedAs("BeltPrefab")]
        [SerializeField]
        private GameObject _beltPrefab;

        [FormerlySerializedAs("Divisor")]
        [SerializeField]
        private GameObject _divisor;

        /// <summary>
        /// The order in which each containerType in the Horizontal layout should display.
        /// </summary>
        [FormerlySerializedAs("HorizontalSlotOrder")]
        [SerializeField]
        private List<ContainerType> _horizontalSlotOrder;

        [FormerlySerializedAs("ClothingLayout")]
        [SerializeField]
        private GameObject _clothingLayout;

        // Slots prefab for the Clothing grid.
        [FormerlySerializedAs("ShoeLeftPrefab")]
        [SerializeField]
        private GameObject _shoeLeftPrefab;

        [FormerlySerializedAs("ShoeRightPrefab")]
        [SerializeField]
        private GameObject _shoeRightPrefab;

        [FormerlySerializedAs("GloveLeftPrefab")]
        [SerializeField]
        private GameObject _gloveLeftPrefab;

        [FormerlySerializedAs("GloveRightPrefab")]
        [SerializeField]
        private GameObject _gloveRightPrefab;

        [FormerlySerializedAs("GlassesPrefab")]
        [SerializeField]
        private GameObject _glassesPrefab;

        [FormerlySerializedAs("MaskPrefab")]
        [SerializeField]
        private GameObject _maskPrefab;

        [FormerlySerializedAs("HeadPrefab")]
        [SerializeField]
        private GameObject _headPrefab;

        [FormerlySerializedAs("JumpsuitPrefab")]
        [SerializeField]
        private GameObject _jumpsuitPrefab;

        [FormerlySerializedAs("ExoSuitPrefab")]
        [SerializeField]
        private GameObject _exoSuitPrefab;

        [FormerlySerializedAs("EarLeftPrefab")]
        [SerializeField]
        private GameObject _earLeftPrefab;

        [FormerlySerializedAs("EarRightPrefab")]
        [SerializeField]
        private GameObject _earRightPrefab;

        [FormerlySerializedAs("DummyPrefab")]
        [SerializeField]
        private GameObject _dummyPrefab;

        private object _lockObject = new object();

        private Dictionary<ContainerType, GameObject> _slotPrefabForType;

        /// <summary>
        /// The order in which clothing slots appear in the grid by container type,
        /// from top left, to bottom right.
        /// </summary>
        [FormerlySerializedAs("ClothingSlotPosition")]
        [SerializeField]
        private List<ContainerType> _clothingSlotPosition;

        // The number of Hand slots.
        public int CountHandsSlots => _slots.Count(x => x.Container.ContainerType == ContainerType.Hand);

        public void Setup(HumanInventory inventory)
        {
            FillSlotPrefabDictionnary();
            FillClothingLayoutWithDummySlots();
            _inventory = inventory;

            foreach (AttachedContainer container in inventory.Containers)
            {
                HandleInventoryContainerAdded(container);
            }

            inventory.OnInventoryContainerAdded += HandleInventoryContainerAdded;
            inventory.OnInventoryContainerRemoved += HandleInventoryContainerRemoved;
        }

        /// <summary>
        /// Get the transform of a hand slot game object.
        /// </summary>
        /// <param name="index"> The index of the hand slot, necessary as multiple hand slots can be on a player. </param>
        /// <returns> The transform of the hand slot at the specified index.</returns>
        public Transform GetHandSlot(Hand hand)
        {
            SingleItemContainerSlot slot = _slots.Find(x => x.Container == hand.Container);
            return slot.transform;
        }

        public void DestroyAllSlots()
        {
            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                _slots[i].gameObject.Dispose(true);
            }

            _slots.Clear();
        }

        private bool HasSlotOfType(ContainerType type)
        {
            return _slots.Any(x => x.Container.ContainerType == type);
        }

        private bool HasSlotWithHigherOrSameOrderThan(ContainerType type)
        {
            return _slots.Any(x => OrderOfType(x.Container.ContainerType) >= OrderOfType(type));
        }

        /// <summary>
        /// Necessary method to set up the clothing slot grid. It fills the whole grid with invisible dummy slots.
        /// </summary>
        [Client]
        private void FillClothingLayoutWithDummySlots()
        {
            for (int i = 0; i < _clothingSlotPosition.Count; i++)
            {
                GameObject dummySlot = Instantiate(_dummyPrefab);
                dummySlot.transform.SetParent(_clothingLayout.transform, false);
                dummySlot.transform.SetAsFirstSibling();
            }
        }

        /// <summary>
        /// Handle adding the right slot to the right place when a container is added to the inventory.
        /// </summary>
        [Client]
        private void HandleInventoryContainerAdded(AttachedContainer container)
        {
            if (_slots.Exists(x => x.Container == container))
            {
                return;
            }

            SingleItemContainerSlot slot;
            switch (container.Type)
            {
                case ContainerType.Hand:
                {
                    slot = AddHandSlot();
                    break;
                }

                case ContainerType.Pocket:
                case ContainerType.Identification:
                case ContainerType.Bag:
                case ContainerType.Belt:
                {
                    slot = AddHorizontalLayoutSlot(_slotPrefabForType[container.Type], container.Type);
                    break;
                }

                case ContainerType.Glasses:
                case ContainerType.Mask:
                case ContainerType.EarLeft:
                case ContainerType.EarRight:
                case ContainerType.Head:
                case ContainerType.ExoSuit:
                case ContainerType.Jumpsuit:
                case ContainerType.GloveLeft:
                case ContainerType.GloveRight:
                case ContainerType.ShoeLeft:
                case ContainerType.ShoeRight:
                {
                    slot = AddClothingSlot(_slotPrefabForType[container.Type]);
                    break;
                }

                default:
                {
                    Log.Error(this, $"Unknown or missing container type {container.Type} for this container {container}");
                    slot = null;
                    break;
                }
            }

            if (!slot)
            {
                return;
            }

            slot.Container = container;
            slot.Inventory = _inventory;
            _slots.Add(slot);
        }

        /// <summary>
        /// For a given container type in the Horizontal slot, give it's order from left to right.
        /// </summary>
        private int OrderOfType(ContainerType type)
        {
            return _horizontalSlotOrder.FindIndex(0, x => x == type);
        }

        /// <summary>
        /// Hand slots need a special treatment, as there's left and right hand.
        /// </summary>
        private SingleItemContainerSlot AddHandSlot()
        {
            if (CountHandsSlots % 2 == 0)
            {
                return AddHorizontalLayoutSlot(_handLeftPrefab, ContainerType.Hand);
            }

            return AddHorizontalLayoutSlot(_handRightPrefab, ContainerType.Hand);
        }

        /// <summary>
        /// This place a slot in the horizontal layout according to the order defined
        /// by element order in the HorizontalSlotOrder List of containerType.
        /// In case of multiple slots with the same container type, it places the slot after all other slots
        /// with same container type.
        /// </summary>
        private int PlaceHorizontalLayoutSlot(ContainerType type)
        {
            // if no slot with order higher place at end
            if (!HasSlotWithHigherOrSameOrderThan(type))
            {
                return _horizontalLayout.transform.childCount;
            }

            // if slot with same order, place at last index of slot type.
            if (HasSlotOfType(type))
            {
                return LastIndexSlotOfType(type) + 1;
            }

            // if slot with order lower, place at last index of type just below.
            for (int i = 0; i < _horizontalLayout.transform.childCount; i++)
            {
                Transform childTransform = _horizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && OrderOfType(slot.Container.ContainerType) >= OrderOfType(type))
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
            for (int i = 0; i < _horizontalLayout.transform.childCount; i++)
            {
                Transform childTransform = _horizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.Container.ContainerType == type)
                {
                    if (number == countOfGivenType)
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
            for (int i = 0; i < _horizontalLayout.transform.childCount; i++)
            {
                Transform childTransform = _horizontalLayout.transform.GetChild(i);
                if (childTransform.gameObject.TryGetComponent(out SingleItemContainerSlot slot) && slot.Container.ContainerType == type)
                {
                    slotOfType = slot;
                }
            }

            if (slotOfType == null)
            {
                Log.Warning(this, "no slots of type " + type.ToString() + ", returning index 0 ");
                return 0;
            }

            return slotOfType.gameObject.transform.GetSiblingIndex();
        }

        /// <summary>
        /// Add clothing slot, which are slots in the left/down part of the screen, organised in a grid.
        /// The order of apparition in the grid is from top left to bottom right, and is defined by the ClothingSlotPosition list.
        /// </summary>
        private SingleItemContainerSlot AddClothingSlot(GameObject prefabToInstantiate)
        {
            GameObject clothingSlot = Instantiate(prefabToInstantiate, transform);
            clothingSlot.transform.SetParent(_clothingLayout.transform, false);
            clothingSlot.TryGetComponent(out SingleItemContainerSlot slot);
            int clothPosition = _clothingSlotPosition.FindIndex(0, x => x == slot.ContainerType);

            int slotPositionForContainerType = _clothingSlotPosition.FindIndex(0, x => x == slot.ContainerType);

            Transform currentSlotTransform = _clothingLayout.transform.GetChild(slotPositionForContainerType);

            // Remove the place holder dummy slot first.
            if (currentSlotTransform.gameObject.TryGetComponent(out DummySlot currentSlot))
            {
                currentSlotTransform.transform.SetParent(null, false);
                currentSlotTransform.gameObject.Dispose(true);
            }

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
            slot.transform.SetParent(_horizontalLayout.transform, false);
            slot.transform.SetSiblingIndex(slotIndex);
            return slot.GetComponent<SingleItemContainerSlot>();
        }

        /// <summary>
        /// Remove the corresponding slot when a container is removed from the inventory.
        /// Replace with an empty slot if it's a clothing type of container.
        /// </summary>
        private void HandleInventoryContainerRemoved(AttachedContainer container)
        {
            int indexToRemove = _slots.FindIndex(slot => slot.Container == container);

            // Replace the removed slot with a dummy slot if it's a clothing type of slot.
            // This allow the grid layout elements to keep their positions despite having a slot removed.
            if (_clothingSlotPosition.Contains(container.Type))
            {
                GameObject dummySlot = Instantiate(_dummyPrefab);
                dummySlot.transform.SetParent(_clothingLayout.transform, false);
                int clothPosition = _clothingSlotPosition.FindIndex(0, x => x == _slots[indexToRemove].Container.ContainerType);
                dummySlot.transform.SetSiblingIndex(clothPosition);
            }

            SingleItemContainerSlot slot = _slots[indexToRemove];

            if (slot == null)
            {
                return;
            }

            slot.gameObject.Dispose(true);
            _slots.RemoveAt(indexToRemove);
        }

        private void FillSlotPrefabDictionnary()
        {
            _slotPrefabForType = new()
            {
                { ContainerType.Pocket, _pocketPrefab },
                { ContainerType.Bag, _bagPrefab },
                { ContainerType.Identification, _idSlotPrefab },
                { ContainerType.ShoeLeft, _shoeLeftPrefab },
                { ContainerType.ShoeRight, _shoeRightPrefab },
                { ContainerType.Glasses, _glassesPrefab },
                { ContainerType.Mask, _maskPrefab },
                { ContainerType.Head, _headPrefab },
                { ContainerType.Jumpsuit, _jumpsuitPrefab },
                { ContainerType.ExoSuit, _exoSuitPrefab },
                { ContainerType.GloveLeft, _gloveLeftPrefab },
                { ContainerType.GloveRight, _gloveRightPrefab },
                { ContainerType.EarLeft, _earLeftPrefab },
                { ContainerType.EarRight, _earRightPrefab },
                { ContainerType.Belt, _beltPrefab },
            };
        }
    }
}
