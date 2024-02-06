using Coimbra;
using DynamicPanels;
using QuikGraph;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static SS3D.Systems.Crafting.CraftingRecipe;
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



        protected override void OnStart()
        {
            base.OnStart();
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
        public void DisplayMenu(List<CraftingInteraction> interactions, InteractionEvent interactionEvent, InteractionReference reference)
        {
            ClearGrid();

            foreach (CraftingInteraction interaction in interactions)
            {
                Instantiate(_textSlotPrefab, _textSlotArea.transform, true).GetComponent<CraftingAssetSlot>().Setup(interaction, interactionEvent);
            }

            ShowUI(true);
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
        public void SetSelectedInteraction(CraftingInteraction interaction, InteractionEvent interactionEvent)
        {

            ClearPictures();

            _interaction = interaction;
            _interactionEvent = interactionEvent;

            _objectTitle.text = _interaction.ChosenLink.Target.Name;

            GenericObjectSo asset;

            if (_interaction.ChosenLink.Target.IsTerminal)
            {
                _interaction.ChosenLink.Target.TryGetResult(out WorldObjectAssetReference result);
                asset = Subsystems.Get<TileSystem>().GetAsset(result.Id);
            }
            else
            {
                asset = Subsystems.Get<TileSystem>().GetAsset(_interaction.ChosenLink.Target.Recipe.Target.Id);
            }

            GameObject _pictureSlot = Instantiate(_pictureSlotPrefab, _pictureSlotArea.transform, true);
            _pictureSlot.GetComponent<AssetSlot>().Setup(asset);
        }

        /// <summary>
        /// Trigger the selected crafting interaction.
        /// </summary>
        public void OnBuildClick()
        {
            if (_interaction == null || _interactionEvent == null) return;
            InteractionReference reference = _interactionEvent.Source.Interact(_interactionEvent, _interaction);
            _interactionEvent.Source.ClientInteract(_interactionEvent, _interaction, reference);
        }
    }
}
