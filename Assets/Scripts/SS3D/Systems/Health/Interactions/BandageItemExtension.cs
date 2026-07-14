using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
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
