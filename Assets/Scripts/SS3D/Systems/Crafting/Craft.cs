using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using SS3D.Core;

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
            OpenCraftingMenuInteraction openCraftingMenuInteraction = new OpenCraftingMenuInteraction(type);

            foreach (IInteractionTarget target in targets)
            {
                interactions.Add(new InteractionEntry(target, openCraftingMenuInteraction));
            }

        }
    }
}
