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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public abstract class CraftingInteraction : DelayedInteraction, ICraftingInteraction
    {
        private List<IRecipeIngredient> ItemsToConsume;

        protected CraftingRecipe _recipe;

        private ParticleSystem particles;

        private MeshRenderer targetRenderer;

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

            if (!interactionEvent.Target.GetGameObject().TryGetComponent<IWorldObjectAsset>(out var target)) return false;

            if (!InteractionExtensions.RangeCheck(interactionEvent)) return false;

            if (!craftingSystem.TryGetRecipe(this, target, out _recipe)) return false;

            List<IRecipeIngredient> closeItemsFromTarget = craftingSystem.GetCloseItemsFromTarget(interactionEvent.Target.GetGameObject());

            Dictionary<string, int> potentialRecipeElements = craftingSystem.
                ItemListToDictionnaryOfRecipeElements(closeItemsFromTarget);

            if (!craftingSystem.CheckEnoughCloseItemsForRecipe(potentialRecipeElements, _recipe)) return false;

            ItemsToConsume = craftingSystem.BuildListOfItemToConsume(closeItemsFromTarget, _recipe);

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
            if (ItemsToConsume == null)
            {
                Log.Error(this, "List of items to consume is null, call CanInteract first.");
                return;
            }

            Subsystems.TryGet(out CraftingSystem craftingSystem);

            craftingSystem.Craft(craftingInteraction, interactionEvent, ItemsToConsume, _recipe);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            particles.Dispose(true);
            Craft(this, interactionEvent);
        }
    }
}