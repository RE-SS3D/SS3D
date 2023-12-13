using FishNet;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Logging.LogSettings;
using SS3D.Substances;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<ItemId, Dictionary<string, CraftingRecipe>> _recipeOrganiser = new();

        public override void OnStartNetwork()
        {
            // Need to be called both on server and client,
            // they both need access to recipes.
            base.OnStartNetwork();
            FillRecipeOrganiser();
        }

        /// <summary>
        /// organise the recipes in such a way that it'll be easy to sort through the relevant recipes when looking
        /// up which recipes are available for a given interaction and target.
        /// </summary>
        [ServerOrClient]
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

                _recipeOrganiser.TryAdd(recipe.Target, new Dictionary<string, CraftingRecipe>());
                _recipeOrganiser[recipe.Target][recipe.InteractionName] = recipe;
            }
        }

        public bool TryGetRecipe(Interaction craftingInteraction, ICraftable target, out CraftingRecipe recipe)
        {
            if(!_recipeOrganiser.TryGetValue(target.ItemId, out Dictionary<string, CraftingRecipe> dic))
            {
                recipe = null;
                return false;
            }

            if (!dic.TryGetValue(craftingInteraction.GetGenericName(), out CraftingRecipe recipeSearched))
            {
                recipe = null;
                return false;
            }

            recipe = recipeSearched;
            return true;
        }


        /// <summary>
        /// Using a target item, and a list of item to consume, it despawn everything and spawn
        /// spawn the result item. Be careful, this does not do any recipe check of any sort.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recipe"></param>
        /// <param name="itemToConsume"></param>
        [Server]
        public void Craft(InteractionEvent interaction, List<ICraftable> itemToConsume, List<ItemId> result)
        {
            ICraftable craftableTarget = interaction.Target.GetComponent<ICraftable>();
            craftableTarget.Consume();

            foreach (ICraftable item in itemToConsume)
            {
                item.Consume();
            }
            foreach(ItemId id in result)
            {
                ICraftable itemResult =  Assets.Get<GameObject>(AssetDatabases.Items, (int)id).GetComponent<ICraftable>();
                itemResult.Craft(interaction);
            }  
        }

        /// <summary>
        /// Build the list of items we want to consume. Don't add more to
        /// the list than necessary.
        /// </summary>
        public List<ICraftable> BuildListOfItemToConsume(List<ICraftable> closeItemsFromTarget,
            CraftingRecipe recipe)
        {
            List<ICraftable>  ItemsToConsume = new List<ICraftable>();

            Dictionary<ItemId, int> recipeElements = new Dictionary<ItemId, int>(recipe.Elements);

            foreach (ICraftable item in closeItemsFromTarget)
            {
                if (recipeElements.GetValueOrDefault(item.ItemId) <= 0) continue;
                ItemsToConsume.Add(item);
                recipeElements[item.ItemId] -= 1;
            }

            return ItemsToConsume;
        }

        /// <summary>
        /// Find all items in close proximity from the target of the recipe.
        /// TODO : only collider for item ? Should then ensure collider of item is on the
        /// same game object as item script for all items. Would avoid the getInParent.
        /// </summary>
        public List<ICraftable> GetCloseItemsFromTarget(ICraftable target)
        {
            Vector3 center = target.GameObject.transform.position;

            float radius = 0.5f;

            Collider[] hitColliders = Physics.OverlapSphere(center, radius);
            List<ICraftable> closeItemsFromTarget = new List<ICraftable>();

            foreach (Collider hitCollider in hitColliders)
            {
                ICraftable item = hitCollider.GetComponentInParent<ICraftable>();
                if (item == null) continue;
                closeItemsFromTarget.Add(item);
            }

            return closeItemsFromTarget;
        }

        /// <summary>
        /// Check if there's enough items for the recipe.
        /// </summary>
        /// <param name="potentialRecipeElements"> Items that can potentially be used </param>
        /// <param name="recipe"> The recipe for which we want to check items</param>
        /// <returns></returns>
        public bool CheckEnoughCloseItemsForRecipe(Dictionary<ItemId, int> potentialRecipeElements,
            CraftingRecipe recipe)
        {
            // check if there's enough of each item.
            foreach (ItemId item in recipe.Elements.Keys.ToList())
            {
                int potentialRecipeItemCount = potentialRecipeElements.GetValueOrDefault(item);
                if (potentialRecipeItemCount < recipe.Elements[item])
                {
                    return false;
                }
            }

            return true;
        }

        public Dictionary<ItemId, int> ItemListToDictionnaryOfRecipeElements(List<ICraftable> closeItemsFromTarget)
        {
            // Transform the list into a dictionnary of itemsID and counts of items.
            // This is some overhead to allow for fast comparison between recipe and 
            // available items.
            Dictionary<ItemId, int> potentialRecipeElements = closeItemsFromTarget
            .GroupBy(item => item.ItemId)
            .ToDictionary(group => group.Key, group => group.Count());

            return potentialRecipeElements;
        }
    }
}

