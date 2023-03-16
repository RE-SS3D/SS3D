using System;
using System.Linq;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inventory.Containers
{
    [RequireComponent(typeof(Inventory))]
    public sealed class Hands : InteractionSource, IToolHolder, IInteractionRangeLimit, IInteractionOriginProvider
    {
        /// <summary>
        /// Called when the active hand gets changed
        /// </summary>
        public event Action<int> OnHandChanged;

        [HideInInspector]
        public Inventory Inventory;

        public AttachedContainer[] HandContainers;

        private Controls.HotkeysActions _controls;

        public RangeLimit range = new(1.5f, 1);

        /// <summary>
        /// The origin of an x interaction that is performed is provided by this, we use it for range checks
        /// </summary>
        public Transform interactionOrigin;

        /// <summary>
        /// The item held in the active hand
        /// </summary>
        [CanBeNull]
        public Item ItemInHand => ActiveHandContainer!.Items.FirstOrDefault();

        /// <summary>
        /// The currently active hand
        /// </summary>
        [CanBeNull]
        public AttachedContainer ActiveHand => SelectedHandIndex < HandContainers.Length ? HandContainers[SelectedHandIndex] : null;

        /// <summary>
        /// The container of the currently active hand
        /// </summary>
        [CanBeNull]
        public Container ActiveHandContainer => ActiveHand != null ? ActiveHand.Container : null;

        /// <summary>
        /// If the selected hand is empty
        /// </summary>
        public bool SelectedHandEmpty => ActiveHandContainer!.Empty;

        public Vector3 InteractionOrigin => interactionOrigin.position;

        public HandsView HandsView { get; private set; }

        public int SelectedHandIndex { get; private set; }

        protected override void OnStart()
        {
            base.OnStart();

            HandsView = FindObjectOfType<HandsView>(true);
            HandsView.Hands = this;

            SupportsMultipleInteractions = true;

            _controls = Subsystems.Get<InputSubsystem>().Inputs.Hotkeys;

            _controls.SwapHands.performed += HandleSwapHands;
            _controls.Drop.performed += HandleDropHeldItem;
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

            if (item.Container != ActiveHandContainer && item.Container != null)
            {
                item.Container.RemoveItem(item);
            }

            ActiveHandContainer.AddItem(item);
        }

        public bool IsEmpty()
        {
            return ActiveHandContainer.Empty;
        }

        /// <summary>
        /// Command wrappers for inventory actions using the currently held item.
        /// </summary>
        [Server]
        public void DropHeldItem()
        {
            if (SelectedHandEmpty)
            {
                return;
            }

            ActiveHandContainer.Dump();
        }

        [Server]
        public void PlaceHeldItem(Vector3 position, Quaternion rotation)
        {
            if (SelectedHandEmpty)
            {
                return;
            }

            Item item = ItemInHand;
            item.Container = null;
            ItemUtility.Place(item, position, rotation, transform);
        }

        private void HandleSwapHands(InputAction.CallbackContext context)
        {
            if (!IsOwner || !enabled || HandContainers.Length < 1)
            {
                return;
            }

            SelectedHandIndex = (SelectedHandIndex + 1) % HandContainers.Length;
            OnHandChanged?.Invoke(SelectedHandIndex);
            CmdSetActiveHand(SelectedHandIndex);
        }

        /// <summary>
        /// Set the Active hand of the Player to be the AttachedContainer passed in parameter.
        /// Do nothing if the parameter is the already active parameter.
        /// </summary>
        /// <param name="selectedContainer">This AttachedContainer should only be a hand.</param>
        public void SetActiveHand(AttachedContainer selectedContainer)
        {
            if (selectedContainer == ActiveHand)
            {
                return;
            }

            SelectedHandIndex = HandContainers.ToList().IndexOf(selectedContainer);

            if (SelectedHandIndex != -1)
            {
                OnHandChanged?.Invoke(SelectedHandIndex);
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
    }
}