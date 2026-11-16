using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    public sealed class AntitoxinItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private static readonly AntitoxinInteraction Interaction = new();

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
