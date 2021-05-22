using UnityEngine;

namespace SS3D.Engine.Examine
{
    public class ReqMaxRange : AbstractRequirementDecorator
    {
		
		private float Range;
		
		public ReqMaxRange(IExamineRequirement wrapped, float range)
		{
			DecoratedObject = wrapped;
			Range = range;
		}
		
		public override bool MeetsRequirement(GameObject examinator)
		{
			// Range of zero overrides to unlimited range
			if (Range == 0) return true;
			
			// If range is limited, check it.
			GameObject go = GetBaseObject();
			Vector2 target = new Vector2(go.transform.position.x, go.transform.position.z);
			Vector2 source = new Vector2(examinator.transform.position.x, examinator.transform.position.z);
			return Vector2.Distance(target, source) <= Range;
		}
    }
}