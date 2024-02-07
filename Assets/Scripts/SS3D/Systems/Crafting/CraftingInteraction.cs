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
    /// <summary>
    /// Crafting interactions are at the core of the crafting system. They should be mostly created by the crafting menu interaction,
    /// and they are the bridge between player inputs and the crafting system.
    /// </summary>
    public class CraftingInteraction : DelayedInteraction
    {

        private ParticleSystem particles;

        private MeshRenderer targetRenderer;

        /// <summary>
        /// The recipe link associated to this interaction. Crafting interactions are always associated to a recipe link.
        /// </summary>
        protected TaggedEdge<RecipeStep, RecipeStepLink> _chosenLink;

        private List<Coroutine> _coroutines;

        /// <summary>
        /// The transform of the game object executing the crafting interaction, useful to check if the source moved
        /// during the interaction, for example.
        /// </summary>
        private Transform _characterTransform;

        /// <summary>
        /// The start position of the source of the interaction, when the interaction begins.
        /// </summary>
        private Vector3 _startPosition;

        /// <summary>
        /// Type of this interaction, defines which recipe will be available.
        /// </summary>
        private CraftingInteractionType _type;

        /// <summary>
        /// The start position of the source of the interaction, when the interaction begins.
        /// </summary>
        public Vector3 StartPosition => _startPosition;

        /// <summary>
        /// Type of this interaction, defines which recipe will be available.
        /// </summary>
        public CraftingInteractionType CraftingInteractionType => _type;

        /// <summary>
        /// The transform of the game object executing the crafting interaction, useful to check if the source moved
        /// during the interaction, for example.
        /// </summary>
        public Transform CharacterTransform => _characterTransform;

        /// <summary>
        /// The recipe link associated to this interaction. Crafting interactions are always associated to a recipe link.
        /// </summary>
        public TaggedEdge<RecipeStep, RecipeStepLink> ChosenLink => _chosenLink;

        public CraftingInteraction(float delay, Transform characterTransform, CraftingInteractionType type, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
            _type = type;
            _chosenLink = link;
        }

        /// <summary>
        /// Check if the crafting can occur.
        /// TODO : Add more conditions, as they are more things, such as obstacles, that can prevent a crafting interaction to occur.
        /// </summary>
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

        /// <summary>
        /// Add smoke particles around the crafted target during crafting.
        /// </summary>
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