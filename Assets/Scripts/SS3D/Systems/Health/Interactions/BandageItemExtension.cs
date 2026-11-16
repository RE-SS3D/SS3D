using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Adds bandage interactions when this item is used on viable targets.
    /// </summary>
    public class BandageItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private static readonly BandageInteraction Interaction = new();

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
        {
            foreach (IInteractionTarget target in targets)
            {
                if (Interaction.CanInteract(context.WithTarget(target)))
                {
                    interactions.Add(new InteractionEntry(target, Interaction));
                }
            }
        }
    }
}
