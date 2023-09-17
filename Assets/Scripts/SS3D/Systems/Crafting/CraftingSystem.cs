using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Logging.LogSettings;
using SS3D.Substances;
using SS3D.Systems.Roles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Crafting
{

    public class CraftingSystem : NetworkSystem
    {
        /// <summary>
        /// ItemId is the id of the target item of the recipe.
        /// The string is the name of the interaction.
        /// The value is a list of craftingRecipe, sorted by their target and needed interactions.
        /// </summary>
        private Dictionary<ItemId, Dictionary<string, List<CraftingRecipe>>> _recipeOrganiser = new();

        public override void OnStartServer()
        {
            base.OnStartServer();
            FillRecipeOrganiser();
        }

        /// <summary>
        /// organise the recipes in such a way that it'll be easy to sort through the relevant recipes when looking
        /// up which recipes are available for a given interaction and target.
        /// </summary>
        private void FillRecipeOrganiser()
        {
            AssetDatabase recipesDataBase = Assets.GetDatabase(AssetDatabases.CraftingRecipes);
            foreach(Object asset in recipesDataBase.Assets)
            {
                if(asset is not CraftingRecipe)
                {
                    Debug.LogError("Crafting recipe database contains object which are not recipes");
                    continue;
                }
                CraftingRecipe recipe = (CraftingRecipe) asset;
                _recipeOrganiser[recipe.Target.ItemId][recipe.InteractionName].Add(recipe);
            }
        }
    }
}

