using System;
using System.Linq;
using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Inventory.Containers
{
    [RequireComponent(typeof(Inventory))]
    public class HandsAlt : InteractionSource, IToolHolder, IInteractionRangeLimit, IInteractionOriginProvider
    {
        [SerializeField] public AttachedContainer[] HandContainers;
        [SerializeField] private float handRange;
        private Controls.HotkeysActions _controls;

        [NonSerialized]
        public InventoryAlt Inventory;

        public Color SelectedColor;
        private Color _defaultColor;

        public int SelectedHandIndex { get; private set; }
        public RangeLimit range = new(1.5f, 1);
        // the origin of an x interaction that is performed is provided by this, we use it for range checks
        public Transform interactionOrigin;
        // pickup icon that this hand uses when there's a pickup interaction
        // TODO: When AssetData is on, we should update this to not use this
        public Sprite pickupIcon;
        /// <summary>
        /// Called when the active hand gets changed
        /// </summary>
        public event Action<int> OnHandChanged;
        /// <summary>
        /// The item held in the active hand
        /// </summary>
        public Item ItemInHand => SelectedHandContainer.Items.FirstOrDefault();


        /// <summary>
        /// The currently active hand
        /// </summary>
        public AttachedContainer SelectedHand => SelectedHandIndex < HandContainers.Length ? HandContainers[SelectedHandIndex] : null;
        /// <summary>
        /// The container of the currently active hand
        /// </summary>
        public Container SelectedHandContainer => SelectedHand != null ? SelectedHand.Container : null;
        /// <summary>
        /// If the selected hand is empty
        /// </summary>
        public bool SelectedHandEmpty => SelectedHandContainer.Empty;

        protected override void OnAwake()
        {
            base.OnAwake();
            SupportsMultipleInteractions = true;
        }

        protected override void OnStart()
        {
            base.OnStart();

            _controls = Subsystems.Get<InputSystem>().Inputs.Hotkeys;
            _controls.SwapHands.performed += HandleSwapHands;
            _controls.Drop.performed += HandleDropHeldItem;
            SetHandHighlight(SelectedHandIndex, true);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            _controls.SwapHands.performed -= HandleSwapHands;
            _controls.Drop.performed -= HandleDropHeldItem;
        }

        [Server]
        public void Pickup(Item item)
        {
            if (!SelectedHandEmpty)
            {
                return;
            }

            if (item.Container != SelectedHandContainer && item.Container != null)
            {
                item.Container.RemoveItem(item);
            }

            SelectedHandContainer.AddItem(item);
        }

        public bool IsEmpty()
        {
            return SelectedHandContainer.Empty;
        }

        /*
         * Command wrappers for inventory actions using the currently held item
         */
        [Server]
        public void DropHeldItem()
        {
            if (SelectedHandEmpty)
            {
                return;
            }

            SelectedHandContainer.Dump();
        }

        [Server]
        public void PlaceHeldItem(Vector3 position, Quaternion rotation)
        {
            if (SelectedHandEmpty)
            {
                return;
            }

            Item item = ItemInHand;
            item.SetContainer(null);
            ItemUtility.Place(item, position, rotation, transform);
        }

        private void HandleSwapHands(InputAction.CallbackContext context)
        {
            if (!IsOwner || !enabled || HandContainers.Length < 1)
            {
                return;
            }
            int oldSelectedHandIndex = SelectedHandIndex;
            SelectedHandIndex = (SelectedHandIndex + 1) % HandContainers.Length;
            OnHandChanged?.Invoke(SelectedHandIndex);
            HighLightChanged(oldSelectedHandIndex);
            CmdSetActiveHand(SelectedHandIndex);
        }

        /// <summary>
        /// Set the Active hand of the Player to be the AttachedContainer passed in parameter.
        /// Do nothing if the parameter is the already active parameter.
        /// </summary>
        /// <param name="selectedContainer">This AttachedContainer should only be a hand.</param>
        public void SetActiveHand(AttachedContainer selectedContainer)
        {
            if (selectedContainer == SelectedHand)
            {
                return;
            }

            if (!HandContainers.Contains(selectedContainer))
            {
                return;
            }

            int oldSelectedHandIndex = SelectedHandIndex;
            SelectedHandIndex = HandContainers.ToList().IndexOf(selectedContainer);
            if (SelectedHandIndex != -1)
            {
                OnHandChanged?.Invoke(SelectedHandIndex);
                HighLightChanged(oldSelectedHandIndex);
                CmdSetActiveHand(SelectedHandIndex);
            }
            else
            {
                Debug.LogError("selectedContainer is not in HandContainers.");
            }
        }

        private void HandleDropHeldItem(InputAction.CallbackContext context)
        {
            CmdDropHeldItem();
        }
        [ServerRpc]
        private void CmdDropHeldItem()
        {
            DropHeldItem();
        }

        [ServerRpc]
        private void CmdSetActiveHand(int selectedHand)
        {
            if (selectedHand >= 0 && selectedHand < HandContainers.Length)
            {
                SelectedHandIndex = selectedHand;
            }
            else
            {
                Debug.Log($"Invalid hand index {selectedHand}");
            }
        }

        public IInteractionSource GetActiveTool()
        {
            Item itemInHand = ItemInHand;
            if (itemInHand == null)
            {
                return null;
            }

            IInteractionSource interactionSource = itemInHand.Prefab.GetComponent<IInteractionSource>();
            if (interactionSource != null)
            {
                interactionSource.Source = this;
            }
            return interactionSource;
        }
        public RangeLimit GetInteractionRange()
        {
            return range;
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
        }

        public Vector3 InteractionOrigin => interactionOrigin.position;


        //TODO : put that in inventory view ?
        private void HighLightChanged(int oldIndex)
        {
            if (SelectedHandIndex != -1)
            {
                SetHandHighlight(oldIndex, false);
            }

            SetHandHighlight(SelectedHandIndex, true);
        }

        //TODO : put that in inventory view ?
        private void SetHandHighlight(int index, bool highlight)
        {
            Transform child = Inventory.InventoryView.GetHandSlot(index);
            Button button = child.GetComponent<Button>();
            ColorBlock buttonColors = button.colors;
            if (highlight)
            {
                _defaultColor = buttonColors.normalColor;
                buttonColors.normalColor = SelectedColor;
                buttonColors.highlightedColor = SelectedColor; // The selected hand keeps the same color, highlighted or not.
            }
            else
            {
                buttonColors.normalColor = _defaultColor;
            }

            button.colors = buttonColors;
        }
    }
}