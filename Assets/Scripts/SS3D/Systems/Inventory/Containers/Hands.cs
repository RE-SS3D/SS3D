using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Combat;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Inventory.Containers
{

    /// <summary>
    /// Handle selections of the active hands, changing colors of active hand slot, and using controls such as dropping or swapping hands.
    /// Also acts as a controller for all hands present on the player.
    /// </summary>
    [RequireComponent(typeof(HumanInventory))]
    public class Hands : NetworkActor, IHandsController
    {
        /// <summary>
        /// List of hands currently on the player, should be modified on server only.
        /// </summary>
        [SerializeField] public List<Hand> PlayerHands;


        private Controls.HotkeysActions _controls;
        private bool _controlsInitialized;

        /// <summary>
        /// Reference to the inventory linked to Hands.
        /// </summary>
        [NonSerialized]
        public HumanInventory Inventory;

        /// <summary>
        /// The selected hand, should be part of PlayerHands list.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncSelectedHand))]
        private Hand _selectedHand;

        /// <summary>
        /// The currently active hand
        /// </summary>
        public Hand SelectedHand => _selectedHand;

        /// <summary>
        /// A list of all containers linked to all hands on player.
        /// </summary>
        public List<AttachedContainer> HandContainers => PlayerHands.Select(x => x.Container).ToList();

        public override void OnStartServer()
        {
            base.OnStartServer();
            foreach(Hand hand in PlayerHands)
            {
                hand.HandsController = this;
                hand.OnHandDisabled += HandleHandRemoved;
                if (hand.Container != null)
                {
                    hand.Container.OnItemAttached += HandleHandItemAttached;
                }
            }
            // Set the selected hand to be the first available one.
            _selectedHand = PlayerHands.FirstOrDefault();
        }

        /// <summary>
        /// After a two-hand rifle lands in its required hand (pickup/transfer auto-route), make that
        /// hand active so the HUD and verbs match the grip.
        /// </summary>
        [Server]
        private void HandleHandItemAttached(object sender, Item item)
        {
            if (!TwoHandedWeaponRules.RequiresBothHands(item, out HandSide requiredSide))
            {
                return;
            }

            Hand grip = TwoHandedWeaponRules.FindHand(this, requiredSide);
            if (grip == null || sender as AttachedContainer != grip.Container)
            {
                return;
            }

            _selectedHand = grip;
        }

        [Server]
        public void ServerSelectHand(Hand hand)
        {
            if (hand == null || !PlayerHands.Contains(hand))
            {
                return;
            }

            _selectedHand = hand;
        }

        /// <summary>
        /// Sync for clients. Active-hand highlighting now lives on the Main HUD gear strip
        /// (HandsGearStrip.SetActiveHand), driven by MainHudSubSystem watching SelectedHand — nothing
        /// left to do here beyond the SyncVar itself.
        /// </summary>
        public void SyncSelectedHand(Hand oldHand, Hand newHand, bool asServer)
        {
        }

        /// <summary>
        /// Link this hands controller to an inventory. Safe on server and client.
        /// Hotkey bind stays client-only via <see cref="OnInventorySetUp"/>.
        /// </summary>
        public void SetInventory(HumanInventory inventory)
        {
            Inventory = inventory;
            if (!IsClient || inventory == null)
            {
                return;
            }

            inventory.OnInventorySetUp -= OnInventorySetUp;
            inventory.OnInventorySetUp += OnInventorySetUp;
        }

        [Client]
        private void OnInventorySetUp()
        {
            if (_controlsInitialized)
                return;

            // Set up hand related controls.
            _controls = SubSystems.Get<InputSubSystem>().Inputs.Hotkeys;
            _controls.SwapHands.performed += HandleSwapHands;
            _controls.Drop.performed += HandleDropHeldItem;
            _controls.ToggleInternalClothing.performed += HandleTogglePockets;
            _controlsInitialized = true;

            Inventory.OnInventorySetUp -= OnInventorySetUp;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            // Death removes ownership before destroying Hands. Subscription lifetime must not
            // depend on current ownership or callbacks keep a destroyed component alive.
            if (_controlsInitialized)
            {
                _controls.SwapHands.performed -= HandleSwapHands;
                _controls.Drop.performed -= HandleDropHeldItem;
                _controls.ToggleInternalClothing.performed -= HandleTogglePockets;
                _controlsInitialized = false;
            }

            if (Inventory != null)
                Inventory.OnInventorySetUp -= OnInventorySetUp;
        }

        [Client]
        private void HandleSwapHands(InputAction.CallbackContext context)
        {
            // We don't swap hand if there's a single one.
            if (!IsOwner || !enabled || PlayerHands.Count <= 1)
            {
                return;
            }

            int index = PlayerHands.FindIndex(0, x => x == SelectedHand);
            if (index < 0)
            {
                return;
            }

            Hand next = PlayerHands[(index + 1) % PlayerHands.Count];
            if (!CanSelectHand(next))
            {
                return;
            }

            CmdNextHand();
        }

        /// <summary>
        /// Set the Active hand of the Player to be the AttachedContainer passed in parameter.
        /// Do nothing if the parameter is the already active parameter.
        /// </summary>
        /// <param name="selectedContainer">This AttachedContainer should only be a hand.</param>
        [ServerRpc]
        public void CmdSetActiveHand(AttachedContainer selectedContainer)
        {

            Hand hand = PlayerHands.FirstOrDefault(x => x.Container == selectedContainer);

            if (hand == selectedContainer)
            {
                Log.Warning(this, "Hand already selected");
                return;
            }

            if (!HandContainers.Contains(selectedContainer))
            {
                Log.Warning(this, "no hand with the passed container in parameter");
                return;
            }

            if (hand != null)
            {
                if (!CanSelectHand(hand))
                {
                    return;
                }

                _selectedHand = hand;
            }
            else
            {
                Log.Error(this, "selectedContainer is not in HandContainers.");
            }
        }

        [Client]
        private void HandleDropHeldItem(InputAction.CallbackContext context)
        {
            if (!IsOwner || !enabled || SelectedHand == null)
                return;

            // Drop is a Help-mode world verb; Harm is combat-exclusive (melee / future combat chords).
            IIntentProvider intentProvider = GetComponentInParent<IIntentProvider>()
                ?? GetComponent<IIntentProvider>();
            if (intentProvider != null && intentProvider.CurrentIntent != IntentType.Help)
            {
                return;
            }

            InteractionController interactions = GetComponentInParent<InteractionController>();
            if (interactions != null && !interactions.BeginOwnerHandAction(
                    InteractionAnimation.FloorPutDown, InteractionAnimation.DropPointAhead(interactions.transform)))
            {
                return;
            }

            SelectedHand.CmdDropHeldItem();
        }

        /// <summary>
        /// Opens pocket containers via ContainerViewer (StoragePanelHost listens) —
        /// replaces the old internal-clothing UI toggle.
        /// </summary>
        [Client]
        private void HandleTogglePockets(InputAction.CallbackContext context)
        {
            if (!IsOwner || !enabled || Inventory?.containerViewer == null)
            {
                return;
            }

            for (int i = 0; Inventory.TryGetTypeContainer(ContainerType.Pocket, i, out AttachedContainer pocket); i++)
            {
                Inventory.containerViewer.ShowContainerUI(pocket);
            }
        }

        [ServerRpc]
        private void CmdNextHand()
        {
            NextHand();
        }

        [Server]
        private void NextHand()
        {
            if (PlayerHands.Count == 0)
            {
                return;
            }

            int index = PlayerHands.FindIndex(0, x => x == SelectedHand);
            if (index < 0)
            {
                index = 0;
            }

            for (int step = 1; step <= PlayerHands.Count; step++)
            {
                Hand candidate = PlayerHands[(index + step) % PlayerHands.Count];
                if (CanSelectHand(candidate))
                {
                    _selectedHand = candidate;
                    return;
                }
            }
        }

        /// <summary>False while the hand is reserved by a two-hand firearm in the other grip.</summary>
        public bool CanSelectHand(Hand hand)
        {
            return hand != null && !TwoHandedWeaponRules.IsHandReserved(hand, this);
        }

        /// <summary>
        /// The source of interaction is either the active hand or the tool held in active hand.
        /// </summary>
        [ServerOrClient]
        public IInteractionSource GetActiveInteractionSource()
        {
            // If no hand is selected, there's no interaction source.
            if (SelectedHand == null) return null;

            IInteractionSource tool = SelectedHand.GetActiveTool();
            if(tool != null)
            {
                return tool;
            }
            else
            {
                return SelectedHand;
            }
        }

        /// <summary>
        /// Change selected hand if the selected hand is removed.
        /// </summary>
        /// <param name="hand"></param>
        [Server]
        public void HandleHandRemoved(Hand hand)
        {
            if (hand?.Container != null)
            {
                hand.Container.OnItemAttached -= HandleHandItemAttached;
            }

            if (!PlayerHands.Remove(hand))
            {
                return;
            }

            if(PlayerHands.Count == 0)
            {
                _selectedHand = null;
                return;
            }

            if (hand == SelectedHand)
            {
                NextHand();
            }
            
        }

        [Server]
        public void AddHand(Hand hand)
        {
            PlayerHands.Add(hand);
            hand.HandsController = this;
            hand.OnHandDisabled += HandleHandRemoved;
            if (hand.Container != null)
            {
                hand.Container.OnItemAttached += HandleHandItemAttached;
            }

            if(PlayerHands.Count == 1)
            {
                _selectedHand = hand;
            }
        }


    }
}
