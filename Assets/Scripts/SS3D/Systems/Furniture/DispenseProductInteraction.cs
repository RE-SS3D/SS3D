using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class DispenseProductInteraction : Interaction
    {
        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Dispense Can";
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;

            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);
            if (!inRange)
            {
                return false;
            }

            return target is VendingMachine;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : AssetData.Get(InteractionIcons.Take);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            IInteractionTarget target = interactionEvent.Target;

            if (target is VendingMachine vendingMachine)
            {
                vendingMachine.CmdDispenseProduct();
            }
            return false;
        }
    }
}