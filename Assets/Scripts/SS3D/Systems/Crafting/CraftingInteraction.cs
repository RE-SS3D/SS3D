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

        public bool Replace => _replace;

        public CraftingInteraction(float delay, Transform characterTransform)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
        }

        /// <summary>
        /// Checks if this interaction can be executed. 
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // if target is item, check if item out of container.
            // if crafting recipe has result, check if result is an item or placed tile objects. 
            //  if it's an item, no special check.
            // if it's a placed tile object, check if place is free and all.
            // if no result, don't need to do any particular check.


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
                return target.Container == null;

            return true;
        }

        private bool ResultIsValid(InteractionEvent interactionEvent, RecipeStep recipeStep)
        {
            if (!recipeStep.HasResult) return true;

            GameObject recipeResult = recipeStep.Result[0];

            if (recipeResult.TryGetComponent<PlacedTileObject>(out var result))
            {
                _replace = false;

                bool targetIsPlacedTileObject = interactionEvent.Target.GetGameObject().TryGetComponent<PlacedTileObject>(out var target);

                if (targetIsPlacedTileObject && result.Layer == target.Layer)
                {
                    _replace = true;
                }

                return Subsystems.Get<TileSystem>().CanBuild(result.tileObjectSO, interactionEvent.Target.GetGameObject().transform.position, Direction.North, replace);
            }

            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {

        }

    }
}