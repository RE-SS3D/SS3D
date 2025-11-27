using JetBrains.Annotations;
using QuikGraph;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public sealed class OpenCraftingMenuInteraction : IInteraction
    {
        private InteractionType _interactionType;

        public OpenCraftingMenuInteraction(InteractionType craftingInteraction)
        {
            _interactionType = craftingInteraction;
        }

        public InteractionType InteractionType => InteractionType.None;

        [NotNull]
        public string GetGenericName() => "Open crafting menu";

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        /// <summary>
        /// Get the name of the interaction
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        [NotNull]
        public string GetName(InteractionEvent interactionEvent) => "Open crafting menu";

        /// <summary>
        /// Get the icon of the interaction
        /// </summary>
        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return null;
        }

        /// <summary>
        /// Check if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!Subsystems.TryGet(out CraftingSubSystem craftingSystem))
            {
                return false;
            }

            bool recipesAvailable = true;
            recipesAvailable &= craftingSystem.AvailableRecipeLinks(_interactionType, interactionEvent, out List<TaggedEdge<RecipeStep, RecipeStepLink>> _);

            return recipesAvailable;
        }

        /// <summary>
        /// Start the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Subsystems.TryGet(out CraftingSubSystem craftingSystem);
            List<CraftingInteraction> craftingInteractions = craftingSystem.CreateInteractions(interactionEvent, _interactionType);
            ViewLocator.Get<CraftingMenu>()[0].DisplayMenu(craftingInteractions, interactionEvent, reference, _interactionType);

            return true;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
