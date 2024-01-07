using Codice.Client.BaseCommands.CheckIn.Progress;
using Coimbra;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SS3D.Systems.Crafting.CraftingRecipe;
using static UnityEngine.GraphicsBuffer;

namespace SS3D.Systems.Crafting
{
    public abstract class CraftingInteraction : DelayedInteraction
    {

        private ParticleSystem particles;

        private MeshRenderer targetRenderer;

        protected RecipeStep _craftingRecipe;

        protected List<IRecipeIngredient> _itemToConsume;

        private List<Coroutine> _coroutines;

        private Transform _characterTransform;

        private Vector3 _startPosition;

        private bool _replace;

        private CraftingInteractionType _type;

        private bool _craftInHand;

        public Vector3 StartPosition => _startPosition;

        public bool CraftInHand => _craftInHand;

        public bool Replace => _replace;

        public CraftingInteractionType CraftingInteractionType => _type;

        public Transform CharacterTransform => _characterTransform;

        public CraftingInteraction(float delay, Transform characterTransform, CraftingInteractionType type)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
            _type = type;
        }

        /// <summary>
        /// Checks if this interaction can be executed. 
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!Subsystems.TryGet(out CraftingSystem craftingSystem)) return false;

            if (!craftingSystem.CanCraft(this, interactionEvent, out _itemToConsume, out _craftingRecipe)) return false;

            if(!TargetIsValid(interactionEvent)) return false;

            if (!ResultIsValid(interactionEvent, _craftingRecipe)) return false;

            // Check for movement once the interaction started.
            if (HasStarted && !InteractionExtensions.CharacterMoveCheck(_startPosition, _characterTransform.position)) return false;

            if (!InteractionExtensions.RangeCheck(interactionEvent)) return false;

            return true;
            
        }

        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);
            _startPosition = _characterTransform.position;

            AddCraftingSmoke(interactionEvent);

            Subsystems.TryGet(out CraftingSystem craftingSystem);

            _coroutines = craftingSystem.MoveAllObjectsToCraftPoint(
                interactionEvent.Target.GetGameObject().transform.position,
                _itemToConsume.Select(x => x.GameObject).ToList());

            return true;
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            particles.Dispose(true);
            Subsystems.TryGet(out CraftingSystem craftingSystem);
            craftingSystem.CancelMoveAllObjectsToCraftPoint(_coroutines);
            craftingSystem.Craft(this, interactionEvent);
        }

        private void AddCraftingSmoke(InteractionEvent interactionEvent)
        {
            GameObject particleGameObject = GameObject.Instantiate(ParticlesEffects.ConstructionParticle.Prefab, interactionEvent.Target.GetGameObject().transform.position, Quaternion.identity);
            particles = particleGameObject.GetComponent<ParticleSystem>();

            // Get the shape module of the dust cloud particle system
            ParticleSystem.ShapeModule shapeModule = particles.shape;

            // Adjust the shape to match the object's bounds
            targetRenderer = interactionEvent.Target.GetGameObject().GetComponentInChildren<MeshRenderer>();
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
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return GetGenericName() + " " + interactionEvent.Target.GetGameObject().name.Split("(")[0];
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

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            particles.Dispose(true);
            Subsystems.TryGet(out CraftingSystem craftingSystem);
            craftingSystem.CancelMoveAllObjectsToCraftPoint(_coroutines);
        }

        private bool ResultIsValidPlacedTileObject(PlacedTileObject result, InteractionEvent interactionEvent, RecipeStep recipeStep)
        {
            _replace = false;

            bool targetIsPlacedTileObject = interactionEvent.Target.GetGameObject().TryGetComponent<PlacedTileObject>(out var target);

            if (targetIsPlacedTileObject && result.Layer == target.Layer)
            {
                _replace = true;
            }

            return Subsystems.Get<TileSystem>().CanBuild(result.tileObjectSO, interactionEvent.Target.GetGameObject().transform.position, Direction.North, _replace);
        }

        private bool ItemTargetIsValid(InteractionEvent interactionEvent, Item target)
        { 
            // item target is valid if it is in hand holding the item or out of container.
            if (target.Container != null && interactionEvent.Source is Hand hand && hand.Container == target.Container)
            {
                _craftInHand = true;
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