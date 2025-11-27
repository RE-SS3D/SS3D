using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("type")]
        [SerializeField]
        private InteractionType _type;

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            OpenCraftingMenuInteraction openCraftingMenuInteraction = new(_type);

            foreach (IInteractionTarget target in targets)
            {
                interactions.Add(new(target, openCraftingMenuInteraction));
            }
        }
    }
}
