using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Substances;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{

    public class CraftingSystem : NetworkSystem
    {
        /// <summary>
        /// First string is the id of the target object of the recipe (as the WorldObjectAssetReference's id).
        /// The second string is the name of the interaction.
        /// The value is a list of craftingRecipe, sorted by their target and needed interactions.
        /// </summary>
        private Dictionary<string, Dictionary<string, CraftingRecipe>> _recipeOrganiser = new();

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

            foreach (Object asset in recipesDataBase.Assets.Values)
            {
                if (asset is not CraftingRecipe recipe)
                {
                    Debug.LogError("Crafting recipe database contains object which are not recipes");

                    continue;
                }

                _recipeOrganiser.TryAdd(recipe.Target.Id, new Dictionary<string, CraftingRecipe>());
                _recipeOrganiser[recipe.Target.Id][recipe.InteractionName] = recipe;
            }
        }

        public bool TryGetRecipe(IInteraction craftingInteraction, IWorldObjectAsset target, out CraftingRecipe recipe)
        {
            if (!_recipeOrganiser.TryGetValue(target.Asset.Id, out Dictionary<string, CraftingRecipe> dic))
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
        public void Craft(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (!CanCraft(interaction, interactionEvent, out List<IRecipeIngredient> itemToConsume, out CraftingRecipe recipe)) return;

            IRecipeIngredient craftableTarget = interactionEvent.Target.GetGameObject().GetComponent<IRecipeIngredient>();
            
            if(recipe.ConsumeTarget) craftableTarget.Consume();

            foreach (IRecipeIngredient item in itemToConsume)
            {
                item.Consume();
            }
            foreach(GameObject prefab in recipe.Result)
            {
                prefab.GetComponent<ICraftable>()?.Craft(interaction, interactionEvent);
            }  
        }

        public bool CanCraft(IInteraction interaction, InteractionEvent interactionEvent, out List<IRecipeIngredient> itemToConsume, out CraftingRecipe recipe)
        {
            itemToConsume = new List<IRecipeIngredient>();

            recipe = null;

            if (!interactionEvent.Target.GetGameObject().TryGetComponent<IWorldObjectAsset>(out var target)) return false;

            if (!TryGetRecipe(interaction, target, out recipe)) return false;

            List<IRecipeIngredient> closeItemsFromTarget = GetCloseItemsFromTarget(interactionEvent.Target.GetGameObject());

            Dictionary<string, int> potentialRecipeElements = ItemListToDictionnaryOfRecipeElements(closeItemsFromTarget);

            if (!CheckEnoughCloseItemsForRecipe(potentialRecipeElements, recipe)) return false;

            itemToConsume = BuildListOfItemToConsume(closeItemsFromTarget, recipe);

            return true;
        }

        /// <summary>
        /// Build the list of items we want to consume. Don't add more to
        /// the list than necessary.
        /// </summary>
        private List<IRecipeIngredient> BuildListOfItemToConsume(List<IRecipeIngredient> closeItemsFromTarget,
            CraftingRecipe recipe)
        {
            List<IRecipeIngredient>  itemsToConsume = new List<IRecipeIngredient>();

            Dictionary<string, int> recipeElements = new Dictionary<string, int>(recipe.Elements);

            foreach (IRecipeIngredient item in closeItemsFromTarget)
            {
                if (!item.GameObject.TryGetComponent<IWorldObjectAsset>(out var asset)) continue;

                if (recipeElements.GetValueOrDefault(asset.Asset.Id) <= 0)
                {
                    continue;
                }

                itemsToConsume.Add(item);
                recipeElements[asset.Asset.Id] -= 1;
            }

            return itemsToConsume;
        }

        /// <summary>
        /// Find all items in close proximity from the target of the recipe.
        /// TODO : only collider for item ? Should then ensure collider of item is on the
        /// same game object as item script for all items. Would avoid the getInParent.
        /// </summary>
        private List<IRecipeIngredient> GetCloseItemsFromTarget(GameObject target)
        {
            Vector3 center = target.transform.position;

            float radius = 3f;

            Collider[] hitColliders = Physics.OverlapSphere(center, radius);
            List<IRecipeIngredient> closeItemsFromTarget = new List<IRecipeIngredient>();

            foreach (Collider hitCollider in hitColliders)
            {
                IRecipeIngredient item = hitCollider.GetComponentInParent<IRecipeIngredient>();
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
        private bool CheckEnoughCloseItemsForRecipe(Dictionary<string, int> potentialRecipeElements, CraftingRecipe recipe)
        {
            // check if there's enough of each item.
            foreach (string id in recipe.Elements.Keys.ToList())
            {
                int potentialRecipeItemCount = potentialRecipeElements.GetValueOrDefault(id);

                if (potentialRecipeItemCount < recipe.Elements[id])
                {
                    return false;
                }
            }

            return true;
        }

        private Dictionary<string, int> ItemListToDictionnaryOfRecipeElements(List<IRecipeIngredient> closeItemsFromTarget)
        {
            // Transform the list into a dictionnary of itemsID and counts of items.
            // This is some overhead to allow for fast comparison between recipe and 
            // available items.
            Dictionary<string, int> potentialRecipeElements = closeItemsFromTarget
                .GroupBy(x => x.GameObject.GetComponent<IWorldObjectAsset>().Asset.Id)
                .ToDictionary(group => group.Key, group => group.Count());

            return potentialRecipeElements;
        }
    }
}

