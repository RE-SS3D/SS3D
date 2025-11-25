using Coimbra;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using FishNet;
using FishNet.Object;
using JetBrains.Annotations;
using QuikGraph;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Logging;
using SS3D.Systems.Dragging;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Core of the crafting, store and organize recipes, check what can be crafted, hold the actual craft logic.
    /// </summary>
    public sealed class CraftingSubSystem : NetworkSubSystem
    {
        /// <summary>
        /// First string is the id of the target object of the recipe (as the WorldObjectAssetReference's id).
        /// The value is a list of craftingRecipe, for which the target is the key.
        /// </summary>
        private readonly Dictionary<string, List<CraftingRecipe>> _recipeOrganiser = new();

        /// <summary>
        /// Dictionary links crafting interactions to a list of tweeners to cancel them.
        /// </summary>
        private readonly Dictionary<InteractionReference, List<TweenerCore<Vector3, Vector3, VectorOptions>>> _coroutinesOrganiser = new();

        /// <summary>
        /// Dictionary linking crafting interaction references to particles, to start and cancel them.
        /// </summary>
        private readonly Dictionary<InteractionReference, ParticleSystem> _craftingSmokes = new();

        public override void OnStartNetwork()
        {
            // Need to be called both on server and client,
            // they both need access to recipes.
            base.OnStartNetwork();
            FillRecipeOrganiser();
        }

        /// <summary>
        /// Using a target item, and a list of item to consume, it despawn everything and
        /// spawn the result item.
        /// </summary>
        [Server]
        public void Craft(CraftingInteraction interaction, InteractionEvent interactionEvent)
        {
            TaggedEdge<RecipeStep, RecipeStepLink> link = interaction.ChosenLink;

            if (!CanCraftRecipeLink(interactionEvent, link))
            {
                return;
            }

            List<IRecipeIngredient> ingredients = GetIngredientsToConsume(interactionEvent, link);
            IRecipeIngredient recipeTarget = interactionEvent.Target.GetGameObject().GetComponent<IRecipeIngredient>();

            ModifyOrConsumeRecipeTarget(recipeTarget, interaction, interactionEvent, link);

            if (link.Target.TryGetResult(out WorldObjectAssetReference result))
            {
                SpawnOrModifyMainResult(result, interaction, interactionEvent, link);
            }

            if (link.Tag == null)
            {
                Log.Error(this, $"Tag associated to recipe link {link} should not be null");
                return;
            }

            foreach (SecondaryResult secondaryResult in link.Tag.SecondaryResults)
            {
                for (int i = 0; i < secondaryResult.Amount; i++)
                {
                    DefaultCraft(interaction, interactionEvent, secondaryResult.Asset.Prefab, link.Target);
                }
            }

            foreach (IRecipeIngredient item in ingredients)
            {
                item.Consume();
            }
        }

        /// <summary>
        /// Return a list of all available links, fulfilling all crafting conditions.
        /// </summary>
        /// <returns></returns>
        public bool AvailableRecipeLinks(InteractionType interactionType, InteractionEvent interactionEvent, out List<TaggedEdge<RecipeStep, RecipeStepLink>> availableLinks)
        {
            if (!TryGetRecipeLinks(interactionType, interactionEvent.Target.GetGameObject(), out List<TaggedEdge<RecipeStep, RecipeStepLink>> potentialLinks))
            {
                availableLinks = null;
                return false;
            }

            availableLinks = potentialLinks.Where(link => CanCraftRecipeLink(interactionEvent, link)).ToList();
            return availableLinks.Count > 0;
        }

        /// <summary>
        /// Move objects toward the crafting target, at constant speed.
        /// </summary>
        [Server]
        public void MoveAllObjectsToCraftPoint(CraftingInteraction interaction, InteractionEvent interactionEvent, InteractionReference reference)
        {
            List<TweenerCore<Vector3, Vector3, VectorOptions>> coroutines = new();
            Vector3 targetPosition = interactionEvent.Target.GetGameObject().transform.position;
            List<GameObject> ingredientsToConsume = GetIngredientsToConsume(interactionEvent, interaction.ChosenLink).ConvertAll(x => x.GameObject);

            foreach (GameObject go in ingredientsToConsume)
            {
                coroutines.Add(go.transform.DOMove(targetPosition, 0.75f));
            }

            _coroutinesOrganiser.Add(reference, coroutines);
            AddCraftingSmoke(interactionEvent.Target.GetGameObject(), reference.Id);
        }

        /// <summary>
        /// Stop moving objects, should be called when the interaction is cancelled.
        /// </summary>
        [Server]
        public void CancelMoveAllObjectsToCraftPoint(InteractionReference reference)
        {
            // TODO: Remove checks for null
            _coroutinesOrganiser[reference].Where(x => x != null).ToList().ForEach(x => x.Kill());
            CancelCraftingSmoke(reference.Id);
        }

        /// <summary>
        /// Create available interactions for a given interaction type.
        /// </summary>
        [ServerOrClient]
        [NotNull]
        public List<CraftingInteraction> CreateInteractions(InteractionEvent interactionEvent, InteractionType craftingInteractionType)
        {
            List<CraftingInteraction> craftingInteractions = new();

            if (!AvailableRecipeLinks(craftingInteractionType, interactionEvent, out List<TaggedEdge<RecipeStep, RecipeStepLink>> availableRecipes))
            {
                return craftingInteractions;
            }

            foreach (TaggedEdge<RecipeStep, RecipeStepLink> recipeLink in availableRecipes)
            {
                CraftingInteraction interaction = new(
                    recipeLink.Tag.ExecutionTime,
                    interactionEvent.Source.GetRootSource() is IRootTransformProvider rootTransformProvider ? rootTransformProvider.RootTransform : interactionEvent.Source.GameObject.transform,
                    craftingInteractionType,
                    recipeLink);

                craftingInteractions.Add(interaction);
            }

            return craftingInteractions;
        }

        /// <summary>
        /// Organise the recipes in such a way that it'll be easy to sort through the relevant recipes when looking
        /// up which recipes are available for a given interaction and target.
        /// </summary>
        [ServerOrClient]
        private void FillRecipeOrganiser()
        {
            AssetDatabase recipesDataBase = Assets.GetDatabase(AssetDatabases.CraftingRecipes);

            if (!recipesDataBase)
            {
                Log.Error(this, "recipeDatabase is null");
                return;
            }

            foreach (Object asset in recipesDataBase.Assets.Values)
            {
                if (asset is not CraftingRecipe recipe)
                {
                    Log.Error(this, "Crafting recipe database contains object which is not recipe");
                    continue;
                }

                _recipeOrganiser.TryAdd(recipe.Target.Id, new());
                _recipeOrganiser[recipe.Target.Id].Add(recipe);
            }
        }

        /// <summary>
        /// Get all potential recipes links.
        /// </summary>
        private bool TryGetRecipeLinks(InteractionType interactionType, GameObject target, out List<TaggedEdge<RecipeStep, RecipeStepLink>> links)
        {
            links = new();

            if (!target || !target.TryGetComponent(out IWorldObjectAsset targetAssetReference))
            {
                return false;
            }

            if (!targetAssetReference.Asset)
            {
                Log.Warning(this, "world object asset asset reference is null");
                return false;
            }

            if (!_recipeOrganiser.TryGetValue(targetAssetReference.Asset.Id, out List<CraftingRecipe> recipes))
            {
                Log.Information(this, $"no recipes with target's name {targetAssetReference.Asset.Id}");
                return false;
            }

            string currentStepName = CurrentStepName(target);
            links = (
                from potentialRecipe in recipes
                from link in potentialRecipe.GetLinksFromStep(currentStepName)
                where interactionType == link.Tag?.CraftingInteractionType
                select link).ToList();

            if (links.Count == 0)
            {
                Log.Information(this, $"no recipe links matching interaction type {interactionType}, from recipe step {currentStepName} ");
            }

            return links.Count > 0;
        }

        /// <summary>
        /// Get available ingredients meeting all conditions for the recipe.
        /// </summary>
        private List<IRecipeIngredient> GetIngredientsToConsume(InteractionEvent interactionEvent, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            List<IRecipeIngredient> closeItemsFromTarget = GetCloseItemsFromTarget(interactionEvent);
            closeItemsFromTarget = link.Tag?.ApplyIngredientConditions(closeItemsFromTarget);
            closeItemsFromTarget = BuildListOfItemToConsume(closeItemsFromTarget, link.Tag);
            return closeItemsFromTarget;
        }

        private void ModifyOrConsumeRecipeTarget(
            IRecipeIngredient recipeTarget,
            CraftingInteraction interaction,
            InteractionEvent interactionEvent,
            TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            if (!link.Target.IsTerminal)
            {
                // Either apply some crafting on the current target, or do it on new game objects.
                interactionEvent.Target.GetGameObject().GetComponent<ICraftable>()?.Modify(interaction, interactionEvent, link.Target.Name);
            }
            else
            {
                // Important to do that before spawning, or it'll mess with the spawning of tile objects.
                recipeTarget?.Consume();
            }
        }

        private void SpawnOrModifyMainResult(
            WorldObjectAssetReference result,
            CraftingInteraction interaction,
            InteractionEvent interactionEvent,
            TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            GameObject resultInstance;

            if (!result.Prefab)
            {
                Log.Error(this, $"World object reference {result} has no prefab associated");
                return;
            }

            if (link.Target.CustomCraft)
            {
                resultInstance = result.Prefab.GetComponent<ICraftable>()?.Craft(interaction, interactionEvent);
            }
            else
            {
                resultInstance = DefaultCraft(interaction, interactionEvent, result.Prefab, link.Target);
            }

            if (link.Tag == null || !link.Tag.ModifyResult)
            {
                return;
            }

            if (!resultInstance)
            {
                Log.Error(this, "could not craft an instance for the recipe result");
                return;
            }

            resultInstance.GetComponent<ICraftable>()?.Modify(interaction, interactionEvent, link.Target.Name);
        }

        /// <summary>
        /// Return the current step name of whatever game object is passed in parameter.
        /// </summary>
        private string CurrentStepName(GameObject target)
        {
            if (!target.TryGetComponent(out IWorldObjectAsset targetAssetReference))
            {
                Log.Warning(this, $"GameObject {target} has no IWorldObjectAsset component, can't retrieve the current step name");
                return string.Empty;
            }

            if (!targetAssetReference.Asset.Prefab)
            {
                Log.Error(this, $"IWorldObjectAsset {targetAssetReference} has no prefab associated, returning");
                return string.Empty;
            }

            if (target.TryGetComponent(out ICraftable craftableTarget) && craftableTarget.CurrentStepName != targetAssetReference.Asset.Prefab.name)
            {
                return craftableTarget.CurrentStepName;
            }

            return targetAssetReference.Asset.Prefab.name;
        }

        /// <summary>
        /// Check if all conditions are met to craft following a given recipe link.
        /// </summary>
        private bool CanCraftRecipeLink(InteractionEvent interactionEvent, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            // Server only checks
            if (IsServer)
            {
                if (!TargetIsValid(interactionEvent) || !ResultIsValid(interactionEvent, link.Target))
                {
                    return false;
                }
            }

            List<IRecipeIngredient> ingredients = GetIngredientsToConsume(interactionEvent, link);
            Dictionary<string, int> potentialRecipeElements = ItemListToDictionnaryOfRecipeElements(ingredients);

            return CheckEnoughCloseItemsForRecipe(potentialRecipeElements, link.Tag);
        }

        /// <summary>
        /// Build the list of items we want to consume. Don't add more to
        /// the list than necessary.
        /// </summary>
        [NotNull]
        private List<IRecipeIngredient> BuildListOfItemToConsume([NotNull] List<IRecipeIngredient> closeItemsFromTarget, [NotNull] RecipeStepLink recipeStep)
        {
            List<IRecipeIngredient> itemsToConsume = new();
            Dictionary<string, int> recipeElements = new(recipeStep.Elements);

            foreach (IRecipeIngredient item in closeItemsFromTarget)
            {
                if (!item.GameObject.TryGetComponent(out IWorldObjectAsset asset) || recipeElements.GetValueOrDefault(asset.Asset.Id) <= 0)
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
        /// TODO : game object with no active colliders are not detected by overlapSphere
        /// </summary>
        private List<IRecipeIngredient> GetCloseItemsFromTarget(InteractionEvent interactionEvent)
        {
            GameObject target = interactionEvent.Target.GetGameObject();
            List<IRecipeIngredient> closeItemsFromTarget = new();
            AddRecipeIngredientInHand(interactionEvent, closeItemsFromTarget);
            Vector3 center = target.transform.position;
            const float radius = 3f;
            Collider[] results = new Collider[] { };
            int size = Physics.OverlapSphereNonAlloc(center, radius, results);

            foreach (Collider hitCollider in results)
            {
                IRecipeIngredient item = hitCollider.GetComponentInParent<IRecipeIngredient>();

                if (item == null)
                {
                    continue;
                }

                closeItemsFromTarget.Add(item);
            }

            return closeItemsFromTarget.OrderBy(x => Vector3.Distance(x.GameObject.transform.position, center)).ToList();
        }

        /// <summary>
        /// Check if there's enough items for the recipe.
        /// </summary>
        /// <param name="potentialRecipeElements"> Items that can potentially be used </param>
        /// <param name="recipeSteplink"> The recipe for which we want to check items</param>
        /// <returns></returns>
        private bool CheckEnoughCloseItemsForRecipe(Dictionary<string, int> potentialRecipeElements, [NotNull] RecipeStepLink recipeSteplink)
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

        [NotNull]
        private Dictionary<string, int> ItemListToDictionnaryOfRecipeElements([NotNull] IEnumerable<IRecipeIngredient> closeItemsFromTarget)
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
        /// Method that should handle basic spawning for everything.
        /// </summary>
        [Server]
        private GameObject DefaultCraft(CraftingInteraction interaction, InteractionEvent interactionEvent, GameObject prefab, RecipeStep recipeStep)
        {
            GameObject instance;

            // If result is an item held in hand, either put the crafting result in hand or in front of the crafter.
            if (interactionEvent.Target is Item targetItem && interactionEvent.Source is IContainerProvider hand && targetItem.Container == hand.Container)
            {
                instance = DefaultCraftItemHeldInHand(prefab, hand, recipeStep, interaction);
            }

            // If result is a placed tile object, just place it on the tilemap.
            else if (prefab.TryGetComponent(out PlacedTileObject resultTileObject))
            {
                instance = DefaultCraftTileObject(interactionEvent, resultTileObject);
            }
            else if (interactionEvent.Target.GetGameObject().TryGetComponent(out PlacedTileObject _) && prefab.TryGetComponent(out Draggable _))
            {
                instance = Instantiate(prefab);
                instance.transform.position = interactionEvent.Target.GetGameObject().transform.position;
                InstanceFinder.ServerManager.Spawn(instance);
                instance.SetActive(true);
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

        private void AddRecipeIngredientInHand(InteractionEvent interactionEvent, List<IRecipeIngredient> ingredients)
        {
            IInventory inventory = interactionEvent.Source.GameObject.GetComponentInParent<IInventory>();

            if (inventory is null)
            {
                return;
            }

            List<Item> itemsInHand = new();
            foreach (AttachedContainer container in inventory.Containers.Where(x => x.ContainerType == ContainerType.Hand))
            {
                itemsInHand.AddRange(container.Items);
            }

            foreach (Item item in itemsInHand)
            {
                if (item.GameObject.TryGetComponent(out IRecipeIngredient recipeIngredient))
                {
                    ingredients.Add(recipeIngredient);
                }
            }
        }

        /// <summary>
        /// Handles spawning item, when the target is an item held in hand, and the result is whatever.
        /// </summary>>
        [Server]
        private GameObject DefaultCraftItemHeldInHand(GameObject prefab, IContainerProvider hand, RecipeStep recipeStep, CraftingInteraction interaction)
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
        [Server]
        private GameObject DefaultCraftTileObject([NotNull] InteractionEvent interactionEvent, [NotNull] PlacedTileObject resultTileObject)
        {
            bool replace = false;
            const Direction direction = Direction.North;

            if (interactionEvent.Target.GetGameObject().TryGetComponent(out PlacedTileObject targetTileObject)
                && targetTileObject.Layer == resultTileObject.Layer)
            {
                replace = true;
            }

            Subsystems.Get<TileSubSystem>().CurrentMap.PlaceTileObject(
                resultTileObject.TileObjectSo,
                TileHelper.GetClosestPosition(interactionEvent.Target.GetGameObject().transform.position),
                direction,
                false,
                replace,
                false,
                out GameObject instance);

            return instance;
        }

        /// <summary>
        /// Check if the crafting recipe target is valid (well placed, in good conditions... whatever).
        /// </summary>
        [Server]
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
        [Server]
        private bool ResultIsValid(InteractionEvent interactionEvent, RecipeStep recipeStep)
        {
            if (!recipeStep.TryGetResult(out WorldObjectAssetReference recipeResult))
            {
                return true;
            }

            if (recipeResult.Prefab && recipeResult.Prefab.TryGetComponent(out PlacedTileObject result))
            {
                return ResultIsValidPlacedTileObject(result, interactionEvent);
            }

            return true;
        }

        /// <summary>
        /// Check if the result placed object won't conflict with other placed tile objects. Should check collisions too probably.
        /// </summary>
        [Server]
        private bool ResultIsValidPlacedTileObject([NotNull] PlacedTileObject result, [NotNull] InteractionEvent interactionEvent)
        {
            bool replace = false;
            bool targetIsPlacedTileObject = interactionEvent.Target.GetGameObject().TryGetComponent(out PlacedTileObject target);

            if (targetIsPlacedTileObject && result.Layer == target.Layer)
            {
                replace = true;
            }

            return Subsystems.Get<TileSubSystem>().CanBuild(result.TileObjectSo, interactionEvent.Target.GetGameObject().transform.position, Direction.North, replace);
        }

        /// <summary>
        /// Check if a given item is in a valid configuration to be used in a crafting interaction.
        /// </summary>
        [Server]
        private bool ItemTargetIsValid(InteractionEvent interactionEvent, Item target)
        {
            // item target is valid if it is in hand holding the item or out of container.
            if (target.Container && interactionEvent.Source is IContainerProvider hand && hand.Container == target.Container)
            {
                return true;
            }

            if (!target.Container)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add smoke particles around the crafted target during crafting.
        /// </summary>
        [ObserversRpc]
        private void AddCraftingSmoke(GameObject target, int referenceId)
        {
            GameObject particleGameObject = Instantiate(ParticlesEffects.ConstructionParticle.Prefab, target.transform.position, Quaternion.identity);
            ParticleSystem particles = particleGameObject.GetComponent<ParticleSystem>();

            // Get the shape module of the dust cloud particle system
            ParticleSystem.ShapeModule shapeModule = particles.shape;

            // Adjust the shape to match the object's bounds
            MeshRenderer targetRenderer = target.GetComponentInChildren<MeshRenderer>();
            if (targetRenderer)
            {
                shapeModule.enabled = true;
                shapeModule.shapeType = ParticleSystemShapeType.MeshRenderer;
                shapeModule.meshRenderer = targetRenderer;
            }
            else
            {
                Debug.LogWarning("The object to hide does not have a Renderer component.");
            }

            _craftingSmokes.Add(new(referenceId), particles);
        }

        [ObserversRpc]
        private void CancelCraftingSmoke(int referenceId)
        {
            _craftingSmokes[new(referenceId)].Dispose(true);
            _craftingSmokes.Remove(new(referenceId));
        }
    }
}
