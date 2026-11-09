using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Builds the object-step interactions (weld, pry, plate, repair) a held tool or material offers on
    /// a hovered structure, from <see cref="ConstructionRecipes"/>.
    /// </summary>
    public static class ConstructionInteractions
    {
        /// <summary>Discover: one target-bound entry per relevant rule and target.</summary>
        public static void AddSourceInteractions(ConstructionInput input, IInteractionTarget[] targets,
            List<InteractionEntry> interactions, InteractionEvent context)
        {
        }

        /// <summary>Hover outline probe for the event's current target (D11).</summary>
        public static void CollectFor(ConstructionInput input, InteractionEvent interactionEvent, List<IInteraction> into)
        {
        }
    }
}
