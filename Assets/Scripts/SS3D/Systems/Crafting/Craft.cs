using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Interaction source extension script, to make some game object become source of crafting interactions.
    /// More precisely, it triggers opening the crafting menu interactions, which create crafting interaction.
    /// </summary>
    public class Craft : MonoBehaviour, IInteractionSourceExtension
    {
        /// <summary>
        /// The different types of interaction the game object with this component on can support. 
        /// </summary>
        [SerializeField]
        private CraftingInteractionType type;
        
        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            if (!TryGetComponent(out IInteractionSource source))
            {
                return;
            }

            OpenCraftingMenuInteraction openCraftingMenuInteraction = new(type);

            foreach (IInteractionTarget target in targets)
            {
                // Discover only when recipes exist for this target. Unconditional Add made every
                // empty-hand hover show a yellow outline (Drop-like pollution via a non-null Target).
                if (!openCraftingMenuInteraction.CanInteract(new InteractionEvent(source, target)))
                {
                    continue;
                }

                interactions.Add(new InteractionEntry(target, openCraftingMenuInteraction));
            }
        }
    }
}