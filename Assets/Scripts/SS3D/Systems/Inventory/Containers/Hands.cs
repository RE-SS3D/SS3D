using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
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
    [RequireComponent(typeof(HumanInventory))]
    public class Hands : NetworkActor, IHandsController
	{
        [SerializeField] public Hand[] PlayerHands;

        private Controls.HotkeysActions _controls;

        [NonSerialized]
        public HumanInventory Inventory;

        public Color SelectedColor;
        private Color _defaultColor;

        public int SelectedHandIndex { get; private set; }

        /// <summary>
        /// Called when the active hand gets changed
        /// </summary>
        public event Action<int> OnHandChanged;

        /// <summary>
        /// The currently active hand
        /// </summary>
        public Hand SelectedHand => SelectedHandIndex < PlayerHands.Length ? PlayerHands[SelectedHandIndex] : null;

		public List<AttachedContainer> HandContainers => PlayerHands.Select(x => x.Container).ToList();

        public void SetInventory(HumanInventory inventory)
        {
            Inventory = inventory;
            Inventory.OnInventorySetUp += OnInventorySetUp;
        }

        private void OnInventorySetUp()
        {
            SetHandHighlight(SelectedHandIndex, true);

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

            _controls.SwapHands.performed -= HandleSwapHands;
            _controls.Drop.performed -= HandleDropHeldItem;
        }

        private void HandleSwapHands(InputAction.CallbackContext context)
        {
            if (!IsOwner || !enabled || PlayerHands.Length < 1)
            {
                return;
            }
            int oldSelectedHandIndex = SelectedHandIndex;
            SelectedHandIndex = (SelectedHandIndex + 1) % PlayerHands.Length;
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

            int oldSelectedHandIndex = SelectedHandIndex;
            SelectedHandIndex = PlayerHands.ToList().IndexOf(hand);

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
            SelectedHand.DropHeldItem();
        }

        [ServerRpc]
        private void CmdSetActiveHand(int selectedHand)
        {
            if (selectedHand >= 0 && selectedHand < PlayerHands.Length)
            {
                SelectedHandIndex = selectedHand;
            }
            else
            {
                Debug.Log($"Invalid hand index {selectedHand}");
            }
        }

        private void HighLightChanged(int oldIndex)
        {
            if (SelectedHandIndex != -1)
            {
                SetHandHighlight(oldIndex, false);
            }

            SetHandHighlight(SelectedHandIndex, true);
        }

        private void SetHandHighlight(int index, bool highlight)
        {
            Transform handSlot = ViewLocator.Get<InventoryView>().First().GetHandSlot(index);
            Button button = handSlot.GetComponent<Button>();
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

		public IInteractionSource GetActiveInteractionSource()
		{
			var tool = SelectedHand.GetActiveTool();
			if(tool != null)
			{
				return tool;
			}
			else
			{
				return SelectedHand;
			}
		}
	}
}