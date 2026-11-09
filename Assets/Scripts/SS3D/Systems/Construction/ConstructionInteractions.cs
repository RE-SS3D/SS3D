using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Builds the object-step interactions (weld, pry, plate, repair) a held tool or material offers on
    /// a hovered structure, from <see cref="ConstructionRecipes"/>. Target-bound only, and only where the
    /// rule matches the target, per the Discover contract.
    /// </summary>
    public static class ConstructionInteractions
    {
        // Main thread only; discovery and outline are not re-entrant.
        private static readonly List<ConstructionRule> RulesScratch = new(8);

        /// <summary>Discover: one target-bound entry per relevant rule and target.</summary>
        public static void AddSourceInteractions(ConstructionInput input, IInteractionTarget[] targets,
            List<InteractionEntry> interactions, InteractionEvent context)
        {
            if (targets == null)
            {
                return;
            }

            ConstructionRecipes.ForInput(input, cellActions: false, RulesScratch);
            if (RulesScratch.Count == 0)
            {
                return;
            }

            PlacedTileObject seen = null;
            foreach (IInteractionTarget target in targets)
            {
                PlacedTileObject placed = ResolvePlaced(target);
                if (placed == null || placed == seen)
                {
                    continue;
                }

                seen = placed;
                foreach (ConstructionRule rule in RulesScratch)
                {
                    if (ConstructionObjectRules.Matches(rule, placed))
                    {
                        interactions.Add(new InteractionEntry(target, new ConstructionStepInteraction(rule, placed)));
                    }
                }
            }
        }

        /// <summary>Hover outline probe for the event's current target (D11).</summary>
        public static void CollectFor(ConstructionInput input, InteractionEvent interactionEvent, List<IInteraction> into)
        {
            PlacedTileObject placed = ResolvePlaced(interactionEvent?.Target);
            if (placed == null)
            {
                return;
            }

            ConstructionRecipes.ForInput(input, cellActions: false, RulesScratch);
            foreach (ConstructionRule rule in RulesScratch)
            {
                if (ConstructionObjectRules.Matches(rule, placed))
                {
                    into.Add(new ConstructionStepInteraction(rule, placed));
                }
            }
        }

        private static PlacedTileObject ResolvePlaced(IInteractionTarget target)
        {
            GameObject gameObject = target?.GetGameObject();
            return gameObject != null ? gameObject.GetComponentInParent<PlacedTileObject>() : null;
        }
    }
}
