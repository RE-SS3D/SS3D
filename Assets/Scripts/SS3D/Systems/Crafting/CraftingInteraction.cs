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
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public abstract class CraftingInteraction : DelayedInteraction, ICraftingInteraction
    {

        private ParticleSystem particles;

        private MeshRenderer targetRenderer;

        protected CraftingRecipe _craftingRecipe;

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

            if (!craftingSystem.CanCraft(this, interactionEvent, out List<IRecipeIngredient> itemToConsume, out _craftingRecipe)) return false;

            if (!InteractionExtensions.RangeCheck(interactionEvent)) return false;

            return true;
        }

        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);
            GameObject particleGameObject =  GameObject.Instantiate(ParticlesEffects.ConstructionParticle.Prefab, interactionEvent.Target.GetGameObject().transform.position, Quaternion.identity);
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
            Craft(this, interactionEvent);
        }
    }
}