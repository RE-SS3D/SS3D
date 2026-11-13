using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Adds defibrillator interactions when this item is used on viable targets.
    /// </summary>
    public class DefibrillatorItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private static readonly DefibrillatorInteraction Interaction = new();

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
