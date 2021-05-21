using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Examine;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Furniture.Generic
{
    // Handles disposing water and water cups
    public class WaterCooler : InteractionTargetBehaviour, IExaminable
    {
	// water cup prefab
        public GameObject CupPrefab;
	// how many we have now
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
	    // if we have cups left
	    // if we don't have anything on our hands
            // if we have hands, just to make sure
            return NumberOfCups > 0 && interactionEvent.GetSourceItem() == null && interactionEvent.Source.GetHands() != null;
        }

	// creates the actual cup and adds it to your hands
        private void DispenseCup(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            Item cup = ItemHelpers.CreateItem(CupPrefab);
            interactionEvent.Source.GetHands().Pickup(cup);
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
		
		public IExamineData GetData()
		{
			return new DataNameDescription("", $"{NumberOfCups} cups remaining.");
		}
		
    }
}
