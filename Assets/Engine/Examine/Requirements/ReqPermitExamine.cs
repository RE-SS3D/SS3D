using UnityEngine;

namespace SS3D.Engine.Examine
{
	/// This class should be used as the base IExamineRequirement class, to which
	/// all of the AbstractRequirementDecorators are applied. This class always
	/// approves examinination.
    public class ReqPermitExamine : IExamineRequirement
    {
		private GameObject BaseObject;
		
		public ReqPermitExamine(GameObject baseObject)
		{
			BaseObject = baseObject;
		}
		
        public bool CanExamine(GameObject examinator)
		{
			return true;
		}
		
		public GameObject GetBaseObject()
		{
			return BaseObject;
		}		
    }
}