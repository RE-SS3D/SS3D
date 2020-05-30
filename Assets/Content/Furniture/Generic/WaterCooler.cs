using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Furniture.Generic
{
    public class WaterCooler : InteractionTargetBehaviour
    {
        public GameObject CupPrefab;
    
        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = "Dispense cup", Interact = DispenseCup, RangeCheck = true, CanInteractCallback = CanDispenseCup
                }
            };
        }

        private bool CanDispenseCup(InteractionEvent interactionEvent)
        {
            return interactionEvent.GetSourceItem() == null && interactionEvent.Source.GetHands() != null;
        }

        private void DispenseCup(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            Item cup = ItemHelpers.CreateItem(CupPrefab);
            interactionEvent.Source.GetHands().Pickup(cup.gameObject);
        }
    }
}
