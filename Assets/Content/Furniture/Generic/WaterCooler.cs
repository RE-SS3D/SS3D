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
		
		
		private IExamineRequirement requirements;

		public void Start()
		{
			// Populate requirements for this item to be examined.
			requirements = new ReqPermitExamine(gameObject);
			requirements = new ReqMaxRange(requirements, 2.0f);  // Cups remaining only visible from 2 metres.
			requirements = new ReqObstacleCheck(requirements);			
		}



		
	// water cup prefab
        public GameObject CupPrefab;
	// how many we have now
        public int NumberOfCups;
    
        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
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
		
		public IExamineData GetData()
		{
			return new DataNameDescription("", $"{NumberOfCups} cups remaining.");
		}
		
		public IExamineRequirement GetRequirements()
		{
			return requirements;
		}
		
    }
}
