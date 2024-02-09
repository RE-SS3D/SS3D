using Coimbra;
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
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hand = SS3D.Systems.Inventory.Containers.Hand;

namespace SS3D.Systems.Crafting
{

    /// <summary>
    /// Core of the crafting, store and organize recipes, check what can be crafted, hold the actual craft logic.
    /// </summary>

    public class CraftingSystem : NetworkSystem
    {

        /// <summary>
        /// First string is the id of the target object of the recipe (as the WorldObjectAssetReference's id).
        /// The value is a list of craftingRecipe, for which the target is the key.
        /// </summary>
        private Dictionary<string, List<CraftingRecipe>> _recipeOrganiser = new();

        /// <summary>
        /// Dictionnary linking reference to crafting interactions to a list of coroutines, to start and cancel them.
        /// Sould be mostly used to move ingredients on target.
        /// </summary>
        private Dictionary<InteractionReference, List<Coroutine>> _coroutinesOrganiser = new();

        /// <summary>
        /// Dictionnary linking crafting interaction references to particles, to start and cancel them.
        /// </summary>
        private Dictionary<InteractionReference, ParticleSystem> _craftingSmokes = new();

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
        private bool TryGetRecipeLinks(CraftingInteractionType interactionType, GameObject target, out List<TaggedEdge<RecipeStep, RecipeStepLink>> links)
        {
            links = new List<TaggedEdge<RecipeStep, RecipeStepLink>>();

            if (!target.TryGetComponent(out IWorldObjectAsset targetAssetReference)) return false;

            string currentStepName = CurrentStepName(target);

            if (!_recipeOrganiser.TryGetValue(targetAssetReference.Asset.Id, out List<CraftingRecipe> recipes)) return false;

            foreach(CraftingRecipe potentialRecipe in recipes)
            {
                List<TaggedEdge<RecipeStep, RecipeStepLink>> potentialLinks =  potentialRecipe.GetLinksFromStep(currentStepName);

                foreach(TaggedEdge<RecipeStep, RecipeStepLink> link in potentialLinks)
                {
                    if (interactionType == link.Tag.CraftingInteractionType)
                    {
                        links.Add(link);
                    }
                }
            }

            return links.Count > 0;
        }

        /// <summary>
        /// Get available ingredients meeting all conditions for the recipe.
        /// </summary>
        public List<IRecipeIngredient> GetIngredientsToConsume(InteractionEvent interactionEvent, TaggedEdge<RecipeStep, RecipeStepLink> link)
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
        public void Craft(CraftingInteraction interaction, InteractionEvent interactionEvent)
        {
            var link = interaction.ChosenLink;

            if (!CanCraftRecipeLink(interactionEvent, link))  return;

            List<IRecipeIngredient> ingredients = GetIngredientsToConsume(interactionEvent, link);

            IRecipeIngredient recipeTarget = interactionEvent.Target.GetGameObject().GetComponent<IRecipeIngredient>();

            // Either apply some crafting on the current target, or do it on new game objects.
            if (!link.Target.IsTerminal)
            {
                interactionEvent.Target.GetGameObject().GetComponent<ICraftable>()?.Modify(interaction, interactionEvent, link.Target.Name);
            }

            // Important to do that before spawning, or it'll mess with the spawning of tile objects.
            if (link.Target.IsTerminal) recipeTarget?.Consume();

            if(link.Target.TryGetResult(out WorldObjectAssetReference result))
            {
                GameObject resultInstance;

                if (link.Target.CustomCraft)
                    resultInstance = result.Prefab.GetComponent<ICraftable>()?.Craft(interaction, interactionEvent);
                else
                    resultInstance = DefaultCraft(interaction, interactionEvent, result.Prefab, link.Target);

                if (link.Tag.ModifyResult)
                {
                    resultInstance.GetComponent<ICraftable>()?.Modify(interaction, interactionEvent, link.Target.Name);
                }
            }

            foreach(GameObject prefab in link.Tag.SecondaryResults)
            {
                  DefaultCraft(interaction, interactionEvent, prefab, link.Target);
            }

            foreach (IRecipeIngredient item in ingredients)
            {
                item.Consume();
            }

        }

        /// <summary>
        /// Return the current step name of whatever game object is passed in parameter.
        /// </summary>
        private string CurrentStepName(GameObject target)
        {
            if (!target.TryGetComponent(out IWorldObjectAsset targetAssetReference)) return "";

            string rootStepName = targetAssetReference.Asset.Prefab.name;
            string stepName;

            if (target.TryGetComponent(out ICraftable craftableTarget) && craftableTarget.CurrentStepName != rootStepName)
            {
                stepName = craftableTarget.CurrentStepName;
            }
            else
            {
                stepName = targetAssetReference.Asset.Prefab.name;
            }

            return stepName;
        }

        /// <summary>
        /// Return a list of all available links, fulfilling all crafting conditions.
        /// </summary>
        /// <returns></returns>
        public bool AvailableRecipeLinks(CraftingInteractionType interactionType, InteractionEvent interactionEvent,
            out List<TaggedEdge<RecipeStep, RecipeStepLink>> availableLinks)
        {
            availableLinks = new();

            if (!TryGetRecipeLinks(interactionType, interactionEvent.Target.GetGameObject(),
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

        /// <summary>
        /// Check if all conditions are met to craft following a given recipe link.
        /// </summary>
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


        /// <summary>
        /// Move objects toward the crafting target, at constant speed.
        /// </summary>
        [Server]
        public void MoveAllObjectsToCraftPoint(CraftingInteraction interaction,InteractionEvent interactionEvent, InteractionReference reference)
        {
            float distance;
            float speed;

            Vector3 targetPosition = interactionEvent.Target.GetGameObject().transform.position;

            List<GameObject> ingredientsToConsume = 
                GetIngredientsToConsume(interactionEvent, interaction.ChosenLink).Select(x => x.GameObject).ToList();

            
            List<Coroutine> coroutines = new();

            foreach (GameObject go in ingredientsToConsume)
            {
                distance = Vector3.Distance(go.transform.position, targetPosition);
                speed = 5f * distance;
                coroutines.Add(StartCoroutine(MoveObjectToTarget(go.transform, targetPosition, speed)));
            }

            _coroutinesOrganiser.Add(reference, coroutines);

            AddCraftingSmoke(interactionEvent, reference);
        }

        /// <summary>
        /// Stop moving objects, should be called when the interaction is cancelled.
        /// </summary>
        public void CancelMoveAllObjectsToCraftPoint(InteractionReference reference)
        {
            _coroutinesOrganiser[reference].Where(x => x!= null).ToList().ForEach(x => StopCoroutine(x) );

            _craftingSmokes[reference].Dispose(true);
        }

        /// <summary>
        /// Should be called by a coroutine, moves another gameObject to a specific target, at a given speed
        /// </summary>
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

        /// <summary>
        /// Method that should handle basic spawning for everything.
        /// </summary>
        private GameObject DefaultCraft(CraftingInteraction interaction, InteractionEvent interactionEvent, GameObject prefab, RecipeStep recipeStep)
        {
            GameObject instance;

            // If result is an item held in hand, either put the crafting result in hand or in front of the crafter.
            if (interactionEvent.Target is Item targetItem && interactionEvent.Source is Hand hand &&
                    targetItem.Container == hand.Container)
            {
                instance = DefaultCraftItemHeldInHand(prefab, hand, recipeStep, interaction);
            }

            // If result is a placed tile object, just place it on the tilemap.
            else if(prefab.TryGetComponent(out PlacedTileObject resultTileObject))
            {
                instance = DefaultCraftTileObject(interactionEvent, resultTileObject);
            }

            else
            {
                instance = Instantiate(prefab);
                Vector3 characterGround = interaction.CharacterTransform.position;
                characterGround.y = 0.1f;
                instance.transform.position = characterGround + interaction.CharacterTransform.forward;
                InstanceFinder.ServerManager.Spawn(instance);
                instance.SetActive(true);
            }

            return instance;

        }

        /// <summary>
        /// Handles spawning item, when the target is an item held in hand, and the result is whatever.
        /// </summary>>
        private GameObject DefaultCraftItemHeldInHand(GameObject prefab, Hand hand, RecipeStep recipeStep, CraftingInteraction interaction)
        {
            GameObject instance;

            if (prefab.TryGetComponent(out Item resultItem))
            {
                instance = Instantiate(prefab);

                // If result is an item, replace whatever is in hand by the new item.
                if (recipeStep.IsTerminal)
                {
                    hand.Container.Dump();
                    hand.Container.AddItem(resultItem);
                }
                else
                {
                    Vector3 characterGround = interaction.CharacterTransform.position;
                    characterGround.y = 0;
                    instance.transform.position = characterGround + interaction.CharacterTransform.forward;
                }
                InstanceFinder.ServerManager.Spawn(instance);
            }
            else
            {
                instance = Instantiate(prefab);

                Vector3 characterGround = interaction.CharacterTransform.position;
                characterGround.y = 0;
                instance.transform.position = characterGround + interaction.CharacterTransform.forward;

                InstanceFinder.ServerManager.Spawn(instance);
                instance.SetActive(true);
            }

            return instance;
        }

        /// <summary>
        /// The default method to craft new tile objects.
        /// </summary>
        private GameObject DefaultCraftTileObject(InteractionEvent interactionEvent, PlacedTileObject resultTileObject)
        {
            GameObject instance;

            bool replace = false;
            Direction direction = Direction.North;

            if (interactionEvent.Target.GetGameObject().TryGetComponent(out PlacedTileObject targetTileObject)
                && targetTileObject.Layer == resultTileObject.Layer)
            {
                replace = true;
            }

            Subsystems.Get<TileSystem>().CurrentMap.PlaceTileObject(resultTileObject.tileObjectSO,
                TileHelper.GetClosestPosition(interactionEvent.Target.GetGameObject().transform.position),
                direction, false, replace, false, out instance);

            return instance;
        }
        
        /// <summary>
        /// Check if the crafting recipe target is valid (well placed, in good conditions... whatever).
        /// </summary>
        private bool TargetIsValid(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Item target)
            {
                return ItemTargetIsValid(interactionEvent, target);
            }

            return true;
        }

        /// <summary>
        /// Check if the result of the crafting recipe is valid. Valid depends on the type of the result.
        /// </summary>
        private bool ResultIsValid(InteractionEvent interactionEvent, RecipeStep recipeStep)
        {
            if (!recipeStep.TryGetResult(out WorldObjectAssetReference recipeResult)) return true;

            if (recipeResult.Prefab.TryGetComponent(out PlacedTileObject result))
            {
                return ResultIsValidPlacedTileObject(result, interactionEvent, recipeStep);
            }

            return true;
        }

        /// <summary>
        /// Check if the result placed object won't conflict with other placed tile objects. Should check collisions too probably.
        /// </summary>
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

        /// <summary>
        /// Check if a given item is in a valid configuration to be used in a crafting interaction.
        /// </summary>
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

        /// <summary>
        /// Add smoke particles around the crafted target during crafting.
        /// </summary>
        private void AddCraftingSmoke(InteractionEvent interactionEvent, InteractionReference reference)
        {
            GameObject particleGameObject = GameObject.Instantiate(ParticlesEffects.ConstructionParticle.Prefab, interactionEvent.Target.GetGameObject().transform.position, Quaternion.identity);
            ParticleSystem particles = particleGameObject.GetComponent<ParticleSystem>();

            // Get the shape module of the dust cloud particle system
            ParticleSystem.ShapeModule shapeModule = particles.shape;

            // Adjust the shape to match the object's bounds
            MeshRenderer targetRenderer = interactionEvent.Target.GetGameObject().GetComponentInChildren<MeshRenderer>();
            if (targetRenderer != null)
            {
                shapeModule.enabled = true;
                shapeModule.shapeType = ParticleSystemShapeType.MeshRenderer;
                shapeModule.meshRenderer = targetRenderer;
            }
            else
            {
                Debug.LogWarning("The object to hide does not have a Renderer component.");
            }

            _craftingSmokes.Add(reference, particles);
        }
    }
}

