using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Examine;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Furniture.Generic
{
    public class WaterCooler : InteractionTargetBehaviour, IExaminable
    {
        public GameObject CupPrefab;
        public int NumberOfCups;
    
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
            return NumberOfCups > 0 && interactionEvent.GetSourceItem() == null && interactionEvent.Source.GetHands() != null;
        }

        private void DispenseCup(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            Item cup = ItemHelpers.CreateItem(CupPrefab);
            interactionEvent.Source.GetHands().Pickup(cup.gameObject);
            NumberOfCups--;
        }

        public bool CanExamine(GameObject examinator)
        {
            return true;
        }

        public string GetDescription(GameObject examinator)
        {
            return $"{NumberOfCups} cups remaining.";
        }
		
        public virtual string GetName(GameObject examinator)
        {
            return "";
        }		
    }
}
