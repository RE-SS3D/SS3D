using Coimbra;
using FishNet.Object;
using QuikGraph;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SS3D.Systems.Crafting.CraftingRecipe;

namespace SS3D.Systems.Crafting
{
    public abstract class CraftingInteraction : DelayedInteraction
    {

        private ParticleSystem particles;

        private MeshRenderer targetRenderer;

        protected List<TaggedEdge<RecipeStep, RecipeStepLink>> _availableRecipes;

        protected TaggedEdge<RecipeStep, RecipeStepLink> _chosenLink;

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

            if (!craftingSystem.AvailableRecipeLinks(this, interactionEvent, out _availableRecipes)) return false;

            // Check for movement once the interaction started.
            if (HasStarted && !InteractionExtensions.CharacterMoveCheck(_startPosition, _characterTransform.position)) return false;

            if (!InteractionExtensions.RangeCheck(interactionEvent)) return false;

            return true;
        }

        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Subsystems.TryGet(out CraftingSystem craftingSystem);

            if (!craftingSystem.AvailableRecipeLinks(this, interactionEvent, out _availableRecipes)) return true;

            if (_availableRecipes.Count == 1)
            {
                StartCrafting(interactionEvent, _availableRecipes.First());
                return true;
            }
            else
            {
                craftingSystem.CraftingMenu.DisplayMenu(_availableRecipes, this, interactionEvent);
            }

            return true;
        }

        public void StartCrafting(InteractionEvent interactionEvent, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            _chosenLink = link;

            StartCounter();

            _startPosition = _characterTransform.position;

            AddCraftingSmoke(interactionEvent);

            Subsystems.TryGet(out CraftingSystem craftingSystem);

            List<GameObject> ingredientsToConsume = craftingSystem.GetIngredientsToConsume(interactionEvent, link).Select(x => x.GameObject).ToList();

            _coroutines = craftingSystem.MoveAllObjectsToCraftPoint(
                interactionEvent.Target.GetGameObject().transform.position,
                ingredientsToConsume);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            particles.Dispose(true);
            Subsystems.TryGet(out CraftingSystem craftingSystem);
            craftingSystem.CancelMoveAllObjectsToCraftPoint(_coroutines);
            craftingSystem.Craft(this, interactionEvent, _chosenLink);
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

       

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            particles.Dispose(true);
            Subsystems.TryGet(out CraftingSystem craftingSystem);
            craftingSystem.CancelMoveAllObjectsToCraftPoint(_coroutines);
        }


    }
}