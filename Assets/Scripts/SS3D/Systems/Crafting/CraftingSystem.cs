using Codice.CM.Common;
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
using static SS3D.Systems.Crafting.CraftingRecipe;

namespace SS3D.Systems.Crafting
{

    public class CraftingSystem : NetworkSystem
    {
        /// <summary>
        /// First string is the id of the target object of the recipe (as the WorldObjectAssetReference's id).
        /// The value is a list of craftingRecipe, for which the target is the key.
        /// </summary>
        private Dictionary<string, List<CraftingRecipe>> _recipeOrganiser = new();

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


                _recipeOrganiser.TryAdd(recipe.Target.Id, new List<CraftingRecipe>());
                _recipeOrganiser[recipe.Target.Id].Add(recipe);
            }
        }

        private bool TryGetRecipe(IInteraction craftingInteraction, GameObject target, out RecipeStep step)
        {
            step = null;

            int stepNumber = 0;

            if (!target.TryGetComponent<IWorldObjectAsset>(out var targetAssetReference)) return false;

            if (target.TryGetComponent<ICraftable>(out var craftableTarget)) stepNumber = craftableTarget.CurrentStepNumber;

            if (!_recipeOrganiser.TryGetValue(targetAssetReference.Asset.Id, out List<CraftingRecipe> recipes)) return false;


            foreach(CraftingRecipe potentialRecipe in recipes)
            {
                var recipeStep = potentialRecipe.GetStep(stepNumber);
                if(recipeStep == null) continue;

                if(craftingInteraction.GetGenericName() == recipeStep.InteractionName)
                {
                    step = recipeStep;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Using a target item, and a list of item to consume, it despawn everything and 
        /// spawn the result item. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recipe"></param>
        /// <param name="itemToConsume"></param>
        [Server]
        public void Craft(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (!CanCraft(interaction, interactionEvent, out List<IRecipeIngredient> itemToConsume, out RecipeStep recipeStep)) return;

            IRecipeIngredient recipeTarget = interactionEvent.Target.GetGameObject().GetComponent<IRecipeIngredient>();

            if (recipeStep.ConsumeTarget) recipeTarget.Consume();

            foreach (IRecipeIngredient item in itemToConsume)
            {
                item.Consume();
            }

            // Either apply some crafting on the current target, or do it on new game objects.
            if (recipeStep.CraftOnTarget)
            {
                interactionEvent.Target.GetGameObject().GetComponent<ICraftable>()?.Modify(interaction, interactionEvent);
            }
            else
            {
                foreach (GameObject prefab in recipeStep.Result)
                {
                    // TODO : should add a default behavior, just spawning the thing in place.
                    prefab.GetComponent<ICraftable>()?.Craft(interaction, interactionEvent);
                }
            }
 
        }

        /// <summary>
        /// Check if the crafting can occur, by retrieving the recipe and checking enough ingredients are there.
        /// </summary>
        /// <param name="interaction"> the interaction used for the crafting.</param>
        /// <param name="interactionEvent"> Contains some necessary info regarding the interaction occuring.</param>
        /// <param name="itemToConsume"> TODO : should not be public, maybe split this method in two. </param>
        /// <param name="recipeStep"></param>
        /// <returns></returns>
        public bool CanCraft(IInteraction interaction, InteractionEvent interactionEvent, out List<IRecipeIngredient> itemToConsume, out RecipeStep recipeStep)
        {
            itemToConsume = new List<IRecipeIngredient>();
            recipeStep = null;

            if (!TryGetRecipe(interaction, interactionEvent.Target.GetGameObject(), out recipeStep)) return false;

            List<IRecipeIngredient> closeItemsFromTarget = GetCloseItemsFromTarget(interactionEvent.Target.GetGameObject());

            Dictionary<string, int> potentialRecipeElements = ItemListToDictionnaryOfRecipeElements(closeItemsFromTarget);

            if (!CheckEnoughCloseItemsForRecipe(potentialRecipeElements, recipeStep)) return false;

            itemToConsume = BuildListOfItemToConsume(closeItemsFromTarget, recipeStep);

            return true;
        }

        /// <summary>
        /// Build the list of items we want to consume. Don't add more to
        /// the list than necessary.
        /// </summary>
        private List<IRecipeIngredient> BuildListOfItemToConsume(List<IRecipeIngredient> closeItemsFromTarget,
            RecipeStep recipeStep)
        {
            List<IRecipeIngredient>  itemsToConsume = new List<IRecipeIngredient>();

            Dictionary<string, int> recipeElements = new Dictionary<string, int>(recipeStep.Elements);

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
        /// Find all potential recipe ingredients in close proximity from the target of the recipe.
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

            return closeItemsFromTarget.OrderBy(x => Vector3.Distance(x.GameObject.transform.position, center)).ToList();
        }

        /// <summary>
        /// Check if there's enough items for the recipe.
        /// </summary>
        /// <param name="potentialRecipeElements"> Items that can potentially be used </param>
        /// <param name="recipe"> The recipe for which we want to check items</param>
        /// <returns></returns>
        private bool CheckEnoughCloseItemsForRecipe(Dictionary<string, int> potentialRecipeElements, RecipeStep recipeStep)
        {
            // check if there's enough of each item.
            foreach (string id in recipeStep.Elements.Keys.ToList())
            {
                int potentialRecipeItemCount = potentialRecipeElements.GetValueOrDefault(id);

                if (potentialRecipeItemCount < recipeStep.Elements[id])
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
        [Server]
        public List<Coroutine> MoveAllObjectsToCraftPoint(Vector3 targetPosition, List<GameObject> gameObjectsToMove)
        {
            float distance;
            float speed;

            List<Coroutine> coroutines = new();

            foreach (GameObject go in gameObjectsToMove)
            {
                distance = Vector3.Distance(go.transform.position, targetPosition);
                speed = 5f * distance;
                coroutines.Add(StartCoroutine(MoveObjectToTarget(go.transform, targetPosition, speed)));
            }

            return coroutines;
        }

        public void CancelMoveAllObjectsToCraftPoint(List<Coroutine> coroutines)
        {
            foreach (Coroutine coroutine in coroutines)
            {
                StopCoroutine(coroutine);
            }
        }

        private System.Collections.IEnumerator MoveObjectToTarget(Transform objTransform, Vector3 targetPosition, float speed)
        {
            while (Vector3.Distance(objTransform.position, targetPosition) > 0.1f)
            {
                float step = speed * Time.deltaTime;
                objTransform.position = Vector3.MoveTowards(objTransform.position, targetPosition, step);
                yield return null;
            }

            objTransform.position = targetPosition;
        }

        private void DefaultCraft()
        {

        }
    }
}

