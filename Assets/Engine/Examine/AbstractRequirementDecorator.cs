using UnityEngine;

namespace SS3D.Engine.Examine
{
	/// This abstract class will be used to decorate ('wrap') the ReqPermitExamine
	/// class. Details of the base Examinable object can be retrieved from that
	/// class also.
    public abstract class AbstractRequirementDecorator : IExamineRequirement
    {
		protected IExamineRequirement DecoratedObject;
		
        public bool CanExamine(GameObject examinator)
		{
			return (MeetsRequirement(examinator) && DecoratedObject.CanExamine(examinator));
		}
		
		public GameObject GetBaseObject()
		{
			return DecoratedObject.GetBaseObject();
		}
		
		public abstract bool MeetsRequirement(GameObject examinator);
		
    }
}