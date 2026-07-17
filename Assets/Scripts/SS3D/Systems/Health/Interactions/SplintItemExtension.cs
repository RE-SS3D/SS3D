using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    public sealed class SplintItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        private static readonly SplintInteraction Interaction = new();

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            Item item = GetComponent<Item>();

            foreach (IInteractionTarget target in targets)
            {
                if (Interaction.CanInteract(new InteractionEvent(item, target)))
                {
                    interactions.Add(new InteractionEntry(target, Interaction));
                }
            }
        }
    }
}
