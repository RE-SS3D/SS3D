using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// Honks a horn. Honking requires the target to be BikeHorn
    /// </summary>
    public class HonkInteraction : IInteraction
    {
        public InteractionType InteractionType => InteractionType.None;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public string GetName(InteractionEvent interactionEvent) => "Honk";

        public string GetGenericName() => "Honk";

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Honk;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (target is not BikeHorn horn)
            {
                return false;
            }

            if (!inRange)
            {
                return false;
            }

            return !horn.IsHonking();
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is BikeHorn horn)
            {
                horn.Honk();
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
