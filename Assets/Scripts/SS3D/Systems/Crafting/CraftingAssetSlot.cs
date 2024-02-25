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
using FishNet;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Script allowing to set up text slots in the crafting menu. Those slots, when clicked on, should trigger a given crafting 
    /// interaction.
    /// </summary>
    public class CraftingAssetSlot : MonoBehaviour
    {

        private int _index;
        /// <summary>
        /// Set up the name of the recipe step in a UI slot for the crafting menu. 
        /// </summary>
        public void Setup(string recipeStepName, int index)
        {
            _index = index;
            GetComponentInChildren<TMP_Text>().text = recipeStepName;
        }

        /// <summary>
        /// Called when clicking on the button linked to this slot. Set selected interaction in the crafting menu.
        /// </summary>
        public void OnClick()
        {
            ViewLocator.Get<CraftingMenu>().First().RpcSetSelectedInteraction(_index);
        }
    }
}
