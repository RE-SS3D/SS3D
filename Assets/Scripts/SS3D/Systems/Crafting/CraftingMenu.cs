﻿using Coimbra;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using InputSystem = SS3D.Systems.Inputs.InputSystem;
using NetworkView = SS3D.Core.Behaviours.NetworkView;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Main script orchestrating displaying the crafting menu, triggering interactions when the player interacts with it.
    /// The crafting menu should work this way currently : Select the thing you want to craft in a name list, once selected,
    /// it should show all icons of things crafted. When clicking build button, it crafts the thing.
    /// </summary>
    public class CraftingMenu : NetworkView, IPointerEnterHandler, IPointerExitHandler
    {
        private InputSystem _inputSystem;

        /// <summary>
        ///  The model for a single slot, to display recipe step names in the crafting menu.
        /// </summary>
        [SerializeField]
        private GameObject _textSlotPrefab;

        /// <summary>
        /// The model for an icon for object that are result of the recipe.
        /// </summary>
        [SerializeField]
        private GameObject _pictureSlotPrefab;

        /// <summary>
        /// Game object parent of the area in the crafting menu where the recipe step names will show up.
        /// </summary>
        [SerializeField]
        private GameObject _textSlotArea;

        /// <summary>
        /// Game object parent of the area in the crafting menu where the recipe results icons will show up.
        /// </summary>
        [SerializeField]
        private GameObject _pictureSlotArea;


        /// <summary>
        /// Selected interaction.
        /// </summary>
        private CraftingInteraction _interaction;

        /// <summary>
        /// Event linked to selected interaction.
        /// </summary>
        private InteractionEvent _interactionEvent;

        /// <summary>
        /// TMP field to display the selected recipe step's name. 
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI _objectTitle;

        /// <summary>
        /// Server only, don't try to access it on client. Hold a list of potential interactions for each client, when
        /// they open the crafting menu.
        /// </summary>
        private Dictionary<NetworkConnection, List<CraftingInteraction>> _interactionsForConnection = new();

        private Dictionary<NetworkConnection, InteractionEvent> _eventForConnection = new();


        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            ShowUI(false);
            _inputSystem = Subsystems.Get<InputSystem>();
        }

        /// <summary>
        /// Called when pointer enter the UI of the menu.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
        }

        /// <summary>
        /// Called when pointer exit the UI of the menu.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
        }

        private void ShowUI(bool isShow)
        {
            gameObject.SetActive(isShow);
        }

        /// <summary>
        /// Method called when the crafting menu is opened, normally only when multiple options are possible
        /// for crafting.
        /// </summary>
        [Server]
        public void DisplayMenu(List<CraftingInteraction> interactions, InteractionEvent interactionEvent, InteractionReference reference)
        {
            _interactionsForConnection.Remove(interactionEvent.Source.NetworkObject.Owner);
            _interactionsForConnection.Add(interactionEvent.Source.NetworkObject.Owner, interactions);

            _eventForConnection.Remove(interactionEvent.Source.NetworkObject.Owner);
            _eventForConnection.Add(interactionEvent.Source.NetworkObject.Owner, interactionEvent);

            List<WorldObjectAssetReference> assets = interactions.Select(x => x.ChosenLink.Target.GetResultOrTarget()).ToList();
            TargetOpenCraftingMenu(interactionEvent.Source.NetworkObject.Owner, assets);
        }

        public void HideMenu()
        {
            ShowUI(false);
        }


        /// <summary>
        /// Clear all recipe step's name and result icons in the crafting menu.
        /// </summary>
        private void ClearGrid()
        {
            for (int i = 0; i < _textSlotArea.transform.childCount; i++)
            {
                _textSlotArea.transform.GetChild(i).gameObject.Dispose(true);
            }

            ClearPictures();
        }

        /// <summary>
        /// Clear the icons in the crafting menu.
        /// </summary>
        private void ClearPictures()
        {
            for (int i = 0; i < _pictureSlotArea.transform.childCount; i++)
            {
                _pictureSlotArea.transform.GetChild(i).gameObject.Dispose(true);
            }
        }

        /// <summary>
        /// Set up a given interaction, which will be called upon clicking on the build button.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RpcSetSelectedInteraction(int index, NetworkConnection conn = null)
        {
            _interaction = _interactionsForConnection[conn][index];
            _interactionEvent = _eventForConnection[conn];

            TargetSetVisuals(conn, _interaction.ChosenLink.Target.GetResultOrTarget(), _interaction.ChosenLink.Target.Name);
        }

        /// <summary>
        /// Trigger the selected crafting interaction.
        /// </summary>
        [Client]
        public void OnBuildClick()
        {
            RpcStartSelectedInteraction();
        }

        [TargetRpc]
        private void TargetOpenCraftingMenu(NetworkConnection conn, List<WorldObjectAssetReference> assets)
        {
            ClearGrid();
            int index = 0;
            foreach (WorldObjectAssetReference asset in assets)
            {
                Instantiate(_textSlotPrefab, _textSlotArea.transform, true).GetComponent<CraftingAssetSlot>().Setup(asset, index);
                index++;
            }

            ShowUI(true);
        }

        [TargetRpc]
        private void TargetSetVisuals(NetworkConnection conn, WorldObjectAssetReference result, string nextRecipeStepName)
        {

            ClearPictures();

            GenericObjectSo asset = Subsystems.Get<TileSystem>().GetAsset(result.Id);
            GameObject _pictureSlot = Instantiate(_pictureSlotPrefab, _pictureSlotArea.transform, true);
            _pictureSlot.GetComponent<AssetSlot>().Setup(asset);
            _objectTitle.text = nextRecipeStepName;
        }

        /// <summary>
        /// 
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RpcStartSelectedInteraction()
        {
            if (_interaction == null || _interactionEvent == null) return;
            InteractionReference reference = _interactionEvent.Source.Interact(_interactionEvent, _interaction);
            _interactionEvent.Source.ClientInteract(_interactionEvent, _interaction, reference);
        }
    }
}
