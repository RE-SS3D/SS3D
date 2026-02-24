using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// Honks a horn. Honking requires the target to be BikeHorn
    /// </summary>
    public class HonkInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Honk";
        }

        public string GetGenericName() => throw new System.NotImplementedException();

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Honk);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (source is not Hand)
            {
                return false;
            }

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
    }
}