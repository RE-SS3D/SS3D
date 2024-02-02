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
    public class CraftingInteraction : DelayedInteraction
    {

        private ParticleSystem particles;

        private MeshRenderer targetRenderer;

        protected TaggedEdge<RecipeStep, RecipeStepLink> _chosenLink;

        protected List<IRecipeIngredient> _itemToConsume;

        private List<Coroutine> _coroutines;

        private Transform _characterTransform;

        private Vector3 _startPosition;

        private CraftingInteractionType _type;

        public Vector3 StartPosition => _startPosition;

        public CraftingInteractionType CraftingInteractionType => _type;

        public Transform CharacterTransform => _characterTransform;

        public TaggedEdge<RecipeStep, RecipeStepLink> ChosenLink => _chosenLink;

        public CraftingInteraction(float delay, Transform characterTransform, CraftingInteractionType type, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
            _type = type;
            _chosenLink = link;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Check for movement once the interaction started.
            if (HasStarted && !InteractionExtensions.CharacterMoveCheck(_startPosition, _characterTransform.position)) return false;

            if (!InteractionExtensions.RangeCheck(interactionEvent)) return false;

            return true;
        }

        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            StartCounter();

            _startPosition = _characterTransform.position;

            AddCraftingSmoke(interactionEvent);

            Subsystems.TryGet(out CraftingSystem craftingSystem);

            List<GameObject> ingredientsToConsume = craftingSystem.GetIngredientsToConsume(interactionEvent, _chosenLink).Select(x => x.GameObject).ToList();

            _coroutines = craftingSystem.MoveAllObjectsToCraftPoint(
                interactionEvent.Target.GetGameObject().transform.position,
                ingredientsToConsume);

            ViewLocator.Get<CraftingMenu>().First().HideMenu();

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

       

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            particles.Dispose(true);
            Subsystems.TryGet(out CraftingSystem craftingSystem);
            craftingSystem.CancelMoveAllObjectsToCraftPoint(_coroutines);
        }


    }
}