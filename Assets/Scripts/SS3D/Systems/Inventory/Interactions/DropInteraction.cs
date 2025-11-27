using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class DropInteraction : IInteraction
    {
        public InteractionType InteractionType => InteractionType.None;

        public string GetGenericName() => "Drop";

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        public string GetName(InteractionEvent interactionEvent) => "Drop";

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Discard;

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public bool CanInteract(InteractionEvent interactionEvent)
        {
            return interactionEvent.Source.GetRootSource() is IContainerProvider;
        }

        /// <summary>
        /// Starts the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source.GetRootSource() is IContainerProvider hand)
            {
                hand.Container.Items.First().GiveOwnership(null);
                hand.Container.Dump();
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
