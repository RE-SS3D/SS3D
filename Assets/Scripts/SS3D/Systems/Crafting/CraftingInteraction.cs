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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SS3D.Systems.Crafting.CraftingRecipe;

namespace SS3D.Systems.Crafting
{
    public abstract class CraftingInteraction : DelayedInteraction, ICraftingInteraction
    {

        private ParticleSystem particles;

        private MeshRenderer targetRenderer;

        protected RecipeStep _craftingRecipe;

        protected List<IRecipeIngredient> _itemToConsume;

        private List<Coroutine> _coroutines;

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!CanCraft(interactionEvent)) return false;

            return true;
        }

        public bool CanCraft(InteractionEvent interactionEvent)
        {
            if (!Subsystems.TryGet(out CraftingSystem craftingSystem)) return false;

            if (!craftingSystem.CanCraft(this, interactionEvent, out _itemToConsume, out _craftingRecipe)) return false;

            if (!InteractionExtensions.RangeCheck(interactionEvent)) return false;

            return true;
        }

        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);
            AddCraftingSmoke(interactionEvent);

            Subsystems.TryGet(out CraftingSystem craftingSystem);

            _coroutines = craftingSystem.MoveAllObjectsToCraftPoint(
                interactionEvent.Target.GetGameObject().transform.position,
                _itemToConsume.Select(x => x.GameObject).ToList());

            return true;
        }

        [Server]
        public void Craft(IInteraction craftingInteraction, InteractionEvent interactionEvent)
        {
            Subsystems.TryGet(out CraftingSystem craftingSystem);

            craftingSystem.Craft(craftingInteraction, interactionEvent);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            particles.Dispose(true);
            Subsystems.TryGet(out CraftingSystem craftingSystem);
            craftingSystem.CancelMoveAllObjectsToCraftPoint(_coroutines);
            Craft(this, interactionEvent);
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

    }
}