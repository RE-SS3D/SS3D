using UnityEngine;

namespace SS3D.Engine.Examine
{
    public class ReqObstacleCheck : AbstractRequirementDecorator
    {
		
		private LayerMask ObstacleMask;
		
		public ReqObstacleCheck(IExamineRequirement wrapped)
		{
			DecoratedObject = wrapped;
			ObstacleMask = LayerMask.GetMask("View Obstacle");
		}
		
		/// Check if line of sight between player and object is blocked by an obstacle.
		public override bool MeetsRequirement(GameObject source)
		{
			GameObject target = GetBaseObject();
			
			// Start by directly checking object and Examinator
			if (!Physics.Linecast(target.transform.position, source.transform.position, ObstacleMask)){return true;}
			
			// If that didn't work, try finding the closest point of the Collider and checking to that.
			Collider targetCollider = target.GetComponent<Collider>();
			if (targetCollider != null)
			{
				Vector3 closestPointOnCollider = targetCollider.ClosestPointOnBounds(source.transform.position);
				if (!Physics.Linecast(closestPointOnCollider, source.transform.position, ObstacleMask)){return true;}
			}
			
			// If it still didn't work, an obstacle blocks the path.
			return false;
		}
    }
}