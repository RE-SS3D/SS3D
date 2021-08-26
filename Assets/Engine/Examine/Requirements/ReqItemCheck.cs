using UnityEngine;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;


namespace SS3D.Engine.Examine
{
    public class ReqItemCheck : AbstractRequirementDecorator
    {
		
		private string ItemID;
		
		public ReqItemCheck(IExamineRequirement wrapped, string itemId)
		{
			DecoratedObject = wrapped;
			ItemID = itemId;
		}
		
		public override bool MeetsRequirement(GameObject examinator)
		{
			// See what's in the player's hand
			Hands hands = examinator.GetComponent<Hands>();
			Item current = hands.ItemInHand;
			
			// If there's nothing there, requirement isn't met.
			if (current == null)
			{
				return false;
			}
			
			// If something's there, see if it matches what we are looking for
			return (current.ItemId == ItemID);
		}
    }
}