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

	/// <summary>
	/// Handle selections of the hands, holding stuff, using tools, and interacting..
	/// Should probably have some of this code in independent hand components, to allow hands to not be usable after loosing one.
	/// </summary>
	/// 
	//TODO Set properly networking on hand selection, hand change should be handled by server only, and then UI change on client only.
    [RequireComponent(typeof(HumanInventory))]
    public class Hands : NetworkActor, IHandsController
	{
		/// <summary>
		/// List of hands currently on the player, should be modified on server only.
		/// </summary>
        [SerializeField] public List<Hand> PlayerHands;


        private Controls.HotkeysActions _controls;

		/// <summary>
		/// Reference to the inventory linked to Hands.
		/// </summary>
        [NonSerialized]
        public HumanInventory Inventory;

		/// <summary>
		/// 
		/// </summary>
		[SerializeField]
        private Color _selectedColor;
		[SerializeField]
		private Color _defaultColor;

		[SyncVar(OnChange = nameof(SyncSelectedHand))]
		private Hand _selectedHand;

		/// <summary>
		/// The currently active hand
		/// </summary>
		public Hand SelectedHand => _selectedHand;


		public List<AttachedContainer> HandContainers => PlayerHands.Select(x => x.Container).ToList();

		public override void OnStartServer()
		{
			base.OnStartServer();
			foreach(Hand hand in PlayerHands)
			{
				hand.handsController = this;
				hand.OnHandDisabled += HandleHandRemoved;
			}

			_selectedHand = PlayerHands.FirstOrDefault();
		}

		public void SyncSelectedHand(Hand oldHand, Hand newHand, bool asServer)
		{
			if (asServer || !IsOwner || oldHand == null || newHand == null) return;
			SetHandHighlight(oldHand, false);
			SetHandHighlight(newHand, true);
		}

		[Client]
		public void SetInventory(HumanInventory inventory)
        {
            Inventory = inventory;
            Inventory.OnInventorySetUp += OnInventorySetUp;
        }

		[Client]
		private void OnInventorySetUp()
        {
			SetHandHighlight(PlayerHands.First(), true);

            _controls = Subsystems.Get<InputSystem>().Inputs.Hotkeys;
            _controls.SwapHands.performed += HandleSwapHands;
            _controls.Drop.performed += HandleDropHeldItem;

            Inventory.OnInventorySetUp -= OnInventorySetUp;
        }

		public IInteractionSource GetActiveTool()
		{
			return SelectedHand.GetActiveTool();
		}

		protected override void OnDestroyed()
        {
            base.OnDestroyed();

			if (IsOwner)
			{
				_controls.SwapHands.performed -= HandleSwapHands;
				_controls.Drop.performed -= HandleDropHeldItem;
			}
        }

		[Client]
        private void HandleSwapHands(InputAction.CallbackContext context)
        {
			// We don't swap hand if there's a single one.
            if (!IsOwner || !enabled || PlayerHands.Count <= 1)
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
				Punpun.Warning(this, "Hand already selected");
                return;
            }

            if (!HandContainers.Contains(selectedContainer))
            {
				Punpun.Warning(this, "no hand with the passed container in parameter");
				return;
            }

            if (hand != null)
            {
                _selectedHand = hand;
            }
            else
            {
                Debug.LogError("selectedContainer is not in HandContainers.");
            }
        }

        private void HandleDropHeldItem(InputAction.CallbackContext context)
        {
            SelectedHand.CmdDropHeldItem();
        }

        [ServerRpc]
        private void CmdNextHand()
        {
			NextHand();
        }

		[Server]
		private void NextHand()
		{
			int index = PlayerHands.FindIndex(0, 1, x => x == SelectedHand);
			_selectedHand = PlayerHands[(index + 1) % PlayerHands.Count];
		}

		[Client]
        private void SetHandHighlight(Hand hand, bool highlight)
        {
			Transform handSlot = ViewLocator.Get<InventoryView>().First().GetHandSlot(hand);
            Button button = handSlot.GetComponent<Button>();
            ColorBlock buttonColors = button.colors;
            if (highlight)
            {
                buttonColors.normalColor = _selectedColor;
                buttonColors.highlightedColor = _selectedColor; // The selected hand keeps the same color, highlighted or not.
            }
            else
            {
                buttonColors.normalColor = _defaultColor;
				buttonColors.highlightedColor = _selectedColor;
			}

            button.colors = buttonColors;
        }

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
			if(PlayerHands.Count == 1)
			{
				_selectedHand = hand;
			}
		}


	}
}