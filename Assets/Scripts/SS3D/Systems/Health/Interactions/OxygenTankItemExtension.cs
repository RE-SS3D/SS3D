using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    public sealed class OxygenTankItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private static readonly OxygenTreatmentInteraction Interaction = new();

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
