using Codice.CM.Common;
using FishNet;
using FishNet.Object;
using QuikGraph;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Substances;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using static QuikGraph.Algorithms.Assignment.HungarianAlgorithm;
using static SS3D.Systems.Crafting.CraftingRecipe;

namespace SS3D.Systems.Crafting
{

    /// <summary>
    /// TODO : Add deconstruction. When deconstructing something, e.g. going from metal window to metal girder with glass sheets, apply all modifications steps to get to the last one.
    /// deconstructing should be as simple as a bool in a recipe. This is necessary as girders don't have glass sheets when they're spawned. Then each step of deconstruction goes
    /// back one step of modification. custom craftable handle that.
    /// </summary>

    public class CraftingSystem : NetworkSystem
    {

        public struct IngredientsForRecipeStepLink
        {
            public List<IRecipeIngredient> _ingredients;
            public RecipeStepLink _link;

            public IngredientsForRecipeStepLink(List<IRecipeIngredient> ingredients, RecipeStepLink link)
            {
                _ingredients = ingredients;
                _link = link;
            }
        }
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

        /// <summary>
        /// Given an interaction and a specific target, get all potential recipes links.
        /// </summary>
        private bool TryGetRecipeLinks(IInteraction interaction, GameObject target, out List<TaggedEdge<RecipeStep, RecipeStepLink>> links)
        {
            links = new List<TaggedEdge<RecipeStep, RecipeStepLink>>();

            string stepName = "";

            if (interaction is not CraftingInteraction craftingInteraction) return false;

            if (!target.TryGetComponent(out IWorldObjectAsset targetAssetReference)) return false;

            if (target.TryGetComponent(out ICraftable craftableTarget)) stepName = craftableTarget.CurrentStepName;

            if (!_recipeOrganiser.TryGetValue(targetAssetReference.Asset.Id, out List<CraftingRecipe> recipes)) return false;


            foreach(CraftingRecipe potentialRecipe in recipes)
            {
                List<TaggedEdge<RecipeStep, RecipeStepLink>> potentialLinks =  potentialRecipe.GetLinksFromStep(stepName);

                foreach(TaggedEdge<RecipeStep, RecipeStepLink> link in potentialLinks)
                {
                    if (craftingInteraction.CraftingInteractionType == link.Tag.CraftingInteractionType)
                    {
                        links.Add(link);
                    }
                }
            }

            return links.Count > 0;
        }

        private List<IRecipeIngredient> GetIngredientsToConsume(InteractionEvent interactionEvent, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            List<IRecipeIngredient> closeItemsFromTarget = GetCloseItemsFromTarget(interactionEvent.Target.GetGameObject());

            closeItemsFromTarget = link.Tag.ApplyIngredientConditions(closeItemsFromTarget);

            closeItemsFromTarget = BuildListOfItemToConsume(closeItemsFromTarget, link.Tag);

            return closeItemsFromTarget;
        }


        /// <summary>
        /// Using a target item, and a list of item to consume, it despawn everything and 
        /// spawn the result item. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recipe"></param>
        /// <param name="itemToConsume"></param>
        [Server]
        public void Craft(CraftingInteraction interaction, InteractionEvent interactionEvent, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            if (!CanCraftRecipeLink(interactionEvent, link))  return;

            List<IRecipeIngredient> ingredients = GetIngredientsToConsume(interactionEvent, link);

            IRecipeIngredient recipeTarget = interactionEvent.Target.GetGameObject().GetComponent<IRecipeIngredient>();

            // Either apply some crafting on the current target, or do it on new game objects.
            if (!link.Target.IsTerminal)
            {
                interactionEvent.Target.GetGameObject().GetComponent<ICraftable>()?.Modify(interaction, interactionEvent);
            }
            else
            {
                foreach (GameObject prefab in link.Target.Result)
                {
                    if(link.Target.CustomCraft)
                        prefab.GetComponent<ICraftable>()?.Craft(interaction, interactionEvent);
                    else
                        DefaultCraft(interaction, interactionEvent, prefab, link.Target);
                }
            }

            if (link.Target.IsTerminal) recipeTarget.Consume();

            foreach (IRecipeIngredient item in ingredients)
            {
                item.Consume();
            }

        }

        /// <summary>
        /// Return a list of all available links, fulfilling all crafting conditions.
        /// </summary>
        /// <returns></returns>
        public bool AvailableRecipeLinks(IInteraction interaction, InteractionEvent interactionEvent,
            out List<TaggedEdge<RecipeStep, RecipeStepLink>> availableLinks)
        {
            availableLinks = new();

            if (!TryGetRecipeLinks(interaction, interactionEvent.Target.GetGameObject(),
                    out List<TaggedEdge<RecipeStep, RecipeStepLink>> potentialLinks))
            {
                return false;
            }

            foreach (TaggedEdge<RecipeStep, RecipeStepLink> link in potentialLinks)
            {
                if (!CanCraftRecipeLink(interactionEvent, link)) continue;
                availableLinks.Add(link);
            }

            return availableLinks.Count > 0; 
        }


        public bool CanCraftRecipeLink(InteractionEvent interactionEvent, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            if (!TargetIsValid(interactionEvent)) return false;

            if (!ResultIsValid(interactionEvent, link.Target)) return false;

            List<IRecipeIngredient> ingredients = GetIngredientsToConsume(interactionEvent, link);

            Dictionary<string, int> potentialRecipeElements = ItemListToDictionnaryOfRecipeElements(ingredients);

            if (!CheckEnoughCloseItemsForRecipe(potentialRecipeElements, link.Tag)) return false;

            return true;
        }

        /// <summary>
        /// Build the list of items we want to consume. Don't add more to
        /// the list than necessary.
        /// </summary>
        private List<IRecipeIngredient> BuildListOfItemToConsume(List<IRecipeIngredient> closeItemsFromTarget,
            RecipeStepLink recipeStep)
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
        private bool CheckEnoughCloseItemsForRecipe(Dictionary<string, int> potentialRecipeElements, RecipeStepLink recipeSteplink)
        {
            // check if there's enough of each item.
            foreach (string id in recipeSteplink.Elements.Keys.ToList())
            {
                int potentialRecipeItemCount = potentialRecipeElements.GetValueOrDefault(id);

                if (potentialRecipeItemCount < recipeSteplink.Elements[id])
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

        private void DefaultCraft(CraftingInteraction interaction, InteractionEvent interactionEvent, GameObject prefab, RecipeStep recipeStep)
        {
            // If result is an item held in hand, either put the crafting result in hand or in front of the crafter.
            if (interactionEvent.Target is Item targetItem && interactionEvent.Source is Hand hand &&
                    targetItem.Container == hand.Container)
            {

                if(prefab.TryGetComponent(out Item resultItem))
                {
                    GameObject instance = Instantiate(prefab);

                    if (recipeStep.IsTerminal)
                    {
                        hand.Container.Dump();
                        hand.Container.AddItem(resultItem);
                    }
                    else
                    {
                        Vector3 characterGround = interaction.CharacterTransform.position;
                        characterGround.y = 0;
                        instance.transform.position = characterGround + interaction.CharacterTransform.forward ;
                    }
                    InstanceFinder.ServerManager.Spawn(instance);
                }
                else
                {
                    GameObject instance = Instantiate(prefab);

                    Vector3 characterGround = interaction.CharacterTransform.position;
                    characterGround.y = 0;
                    instance.transform.position = characterGround + interaction.CharacterTransform.forward;

                    InstanceFinder.ServerManager.Spawn(instance);
                    instance.SetActive(true);
                }
            }

            // If result is a placed tile object, just place it on the tilemap.
            else if(prefab.TryGetComponent(out PlacedTileObject resultTileObject))
            {
                bool replace = false;
                Direction direction = Direction.North;

                if (interaction is CraftingInteraction craftingInteraction)
                {
                    replace = craftingInteraction.Replace;
                }

                Subsystems.Get<TileSystem>().CurrentMap.PlaceTileObject(resultTileObject.tileObjectSO,
                    TileHelper.GetClosestPosition(interactionEvent.Target.GetGameObject().transform.position), direction, false, replace, false);
            }

            else
            {
                GameObject instance = Instantiate(prefab);
                instance.transform.position = interactionEvent.Point;
                InstanceFinder.ServerManager.Spawn(instance);
                instance.SetActive(true);
            }

        }

        private bool TargetIsValid(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Item target)
            {
                return ItemTargetIsValid(interactionEvent, target);
            }

            return true;
        }

        private bool ResultIsValid(InteractionEvent interactionEvent, RecipeStep recipeStep)
        {
            if (!recipeStep.HasResult) return true;

            GameObject recipeResult = recipeStep.Result[0];

            if (recipeResult.TryGetComponent(out PlacedTileObject result))
            {
                return ResultIsValidPlacedTileObject(result, interactionEvent, recipeStep);
            }

            return true;
        }

        private bool ResultIsValidPlacedTileObject(PlacedTileObject result, InteractionEvent interactionEvent, RecipeStep recipeStep)
        {
            bool replace = false;

            bool targetIsPlacedTileObject = interactionEvent.Target.GetGameObject().TryGetComponent<PlacedTileObject>(out var target);

            if (targetIsPlacedTileObject && result.Layer == target.Layer)
            {
                replace = true;
            }

            return Subsystems.Get<TileSystem>().CanBuild(result.tileObjectSO, interactionEvent.Target.GetGameObject().transform.position, Direction.North, replace);
        }

        private bool ItemTargetIsValid(InteractionEvent interactionEvent, Item target)
        {
            // item target is valid if it is in hand holding the item or out of container.
            if (target.Container != null && interactionEvent.Source is Hand hand && hand.Container == target.Container)
            {
                return true;
            }
            else if (target.Container == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

