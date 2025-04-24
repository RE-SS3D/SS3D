using Coimbra;
using FishNet.Connection;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;
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
        private InputSubSystem _inputSystem;

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
        private readonly Dictionary<NetworkConnection, List<CraftingInteraction>> _interactionsForConnection = new();
        
        private readonly Dictionary<NetworkConnection, InteractionEvent> _eventForConnection = new();
        
        private bool _isPointerOnMenu = false;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            ShowUI(false);
            _inputSystem = SubSystems.Get<InputSubSystem>();
        }

        /// <summary>
        /// Called when pointer enter the UI of the menu.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
            _isPointerOnMenu = true;
        }

        /// <summary>
        /// Called when pointer exit the UI of the menu.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
            _isPointerOnMenu = false;
        }

        private void ShowUI(bool isShow)
        {
            gameObject.SetActive(isShow);
        }

        /// <summary>
        /// Method called when the crafting menu is opened, normally only when multiple options are possible for crafting.
        /// </summary>
        [Server]
        public void DisplayMenu(List<CraftingInteraction> interactions, InteractionEvent interactionEvent, InteractionReference reference,
            CraftingInteractionType craftingInteractionType)
        {
            _interactionsForConnection.Remove(interactionEvent.Source.NetworkObject.Owner);
            _interactionsForConnection.Add(interactionEvent.Source.NetworkObject.Owner, interactions);

            _eventForConnection.Remove(interactionEvent.Source.NetworkObject.Owner);
            _eventForConnection.Add(interactionEvent.Source.NetworkObject.Owner, interactionEvent);

            if (interactions.Count == 1)
            {
                _interaction = interactions[0];
                _interactionEvent = interactionEvent;
                StartSelectedInteraction(craftingInteractionType);
            }
            else
            {
                List<string> stepNames = interactions.Select(x => x.ChosenLink.Target.Name).ToList();
                TargetOpenCraftingMenu(interactionEvent.Source.NetworkObject.Owner, stepNames);
                SetSelectedInteraction(0, interactionEvent.Source.NetworkObject.Owner);
            }
        }

        public void HideMenu()
        {
            ShowUI(false);
            
            if (_isPointerOnMenu)
            {
                _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
            }
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
        public void RpcSetSelectedInteraction(int index, [CanBeNull] NetworkConnection conn = null)
        {
            SetSelectedInteraction(index, conn);
        }

        [Server]
        private void SetSelectedInteraction(int index, NetworkConnection conn = null)
        {
            _interaction = _interactionsForConnection[conn][index];
            _interactionEvent = _eventForConnection[conn];
            List<SecondaryResult> results = new();
            
            if (_interaction.ChosenLink.Target.IsTerminal && _interaction.ChosenLink.Target.TryGetResult(out WorldObjectAssetReference result))
            {
                results.Add(new (result, 1));
            }
            else if (!_interaction.ChosenLink.Target.IsTerminal)
            {
                results.Add(new (_interaction.ChosenLink.Target.Recipe.Target, 1));
            }
            
            results.AddRange(_interaction.ChosenLink.Tag.SecondaryResults);
            TargetSetVisuals(conn, results, _interaction.ChosenLink.Target.Name);
        }

        /// <summary>
        /// Trigger the selected crafting interaction.
        /// </summary>
        [Client]
        public void OnBuildClick()
        {
            RpcStartSelectedInteraction();
            ShowUI(false);
        }

        [TargetRpc]
        private void TargetOpenCraftingMenu(NetworkConnection conn, List<string> stepNames)
        {
            ClearGrid();
            int index = 0;
            foreach (string stepName in stepNames)
            {
                Instantiate(_textSlotPrefab, _textSlotArea.transform, true).GetComponent<CraftingAssetSlot>().Setup(stepName, index);
                index++;
            }

            ShowUI(true);
        }

        [TargetRpc]
        private void TargetSetVisuals(NetworkConnection conn, List<SecondaryResult> results, string nextRecipeStepName)
        {
            ClearPictures();

            foreach (SecondaryResult result in results)
            {
                GenericObjectSo asset = SubSystems.Get<TileSubSystem>().GetAsset(result.Asset.Id);
                GameObject pictureSlot = Instantiate(_pictureSlotPrefab, _pictureSlotArea.transform, true);
                pictureSlot.GetComponent<CraftingSlot>().Setup(asset, result.Amount);
            }

            _objectTitle.text = nextRecipeStepName;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcStartSelectedInteraction()
        {
            if (_interaction == null)
            {
                Log.Error(this, "can't start selected interaction, it's null");
                return;
            }
            StartSelectedInteraction(_interaction.CraftingInteractionType);
        }

        [Server]
        private void StartSelectedInteraction(CraftingInteractionType type)
        {
            if (_interaction == null || _interactionEvent == null) 
            {
                Log.Error(this, "can't start selected interaction, it's null, or it's interactionEvent is null");
                return;
            }

            InteractionReference reference = _interactionEvent.Source.Interact(_interactionEvent, _interaction);
            int index  = _interactionsForConnection[_interactionEvent.Source.NetworkObject.Owner].IndexOf(_interaction);
            RpcClientInteract(_interactionEvent.Source.NetworkObject.Owner, _interactionEvent.Target.GetGameObject(), 
                _interactionEvent.Source.GameObject, reference.Id, index, _interaction.CraftingInteractionType);
        }

        /// <summary>
        /// Launch the crafting interaction client side (currently only displaying the loading bar).
        /// </summary>
        /// <param name="conn"> Connection on which to start the client interaction</param>
        /// <param name="target"> Target of the interaction </param>
        /// <param name="sourceObject"> Source of the interaction </param>
        /// <param name="referenceId"> The reference of the interaction, client interaction should be set with the same id as server interaction.</param>
        /// <param name="index"> Index of the interaction, upon creating available interaction from a given interaction event, and a crafting interaction type. </param>
        /// <param name="type"> Type of the chosen crafting interaction, used to retrieve potential interactions.</param>
        [TargetRpc]
        private void RpcClientInteract(NetworkConnection conn, GameObject target, GameObject sourceObject, int referenceId, int index, CraftingInteractionType type)
        {
            SubSystems.TryGet(out CraftingSubSystem craftingSystem);
            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            InteractionEvent interactionEvent = new(source, new InteractionTargetGameObject(target));
            List<CraftingInteraction> craftingInteractions = craftingSystem.CreateInteractions(interactionEvent, type);
            interactionEvent.Source.ClientInteract(interactionEvent, craftingInteractions[index], new(referenceId));
        }
    }
}
