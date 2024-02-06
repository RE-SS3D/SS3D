using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SS3D.Data.AssetDatabases;
using System;
using QuikGraph;
using static SS3D.Systems.Crafting.CraftingRecipe;
using SS3D.Systems.Crafting;
using UnityEngine.AddressableAssets;
using SS3D.Interactions;
using SS3D.Core;
using System.Linq;


namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Script allowing to set up text slots in the crafting menu. Those slots, when clicked on, should trigger a given crafting 
    /// interaction.
    /// </summary>
    public class CraftingAssetSlot : MonoBehaviour
    {
        /// <summary>
        /// Name of the recipe step, or result following the end of the crafting interaction.
        /// </summary>
        private string resultName;

        /// <summary>
        /// Crafting interaction triggered when clicking on this slot.
        /// </summary>
        private CraftingInteraction _interaction;

        /// <summary>
        /// Interaction event from player's interaction, need to be stored to be called if the slot is clicked o.
        /// </summary>
        private InteractionEvent _interactionEvent;


        /// <summary>
        /// Set up the name of the recipe step in a UI slot for the crafting menu. 
        /// </summary>
        public void Setup(CraftingInteraction interaction, InteractionEvent interactionEvent)
        {
            _interaction = interaction;
            _interactionEvent = interactionEvent;

            // Prepare the main result of the recipe if it exist
            if (interaction.ChosenLink.Target.TryGetResult(out WorldObjectAssetReference result))
            {
                resultName = result.Prefab.name;
            }

            // If the link doesn't lead to a new result, show the modified object.
            if (!interaction.ChosenLink.Target.IsTerminal)
            {
                resultName = interaction.ChosenLink.Target.Name;
            }

            GetComponentInChildren<TMP_Text>().text = resultName;
        }

        /// <summary>
        /// Called when clicking on the button linked to this slot. Set selected interaction in the crafting menu.
        /// </summary>
        public void OnClick()
        {
            ViewLocator.Get<CraftingMenu>().First().SetSelectedInteraction(_interaction, _interactionEvent);
        }
    }
}
