using UnityEngine;

namespace SS3D.Engine.Examine
{
    public class SimpleExaminable : MonoBehaviour, IExaminable
    {
        [TextArea(1, 15)]
		[SerializeField]
        public string DisplayName;
        [TextArea(5, 15)]
        public string Text;

        public float MaxDistance;
		
		private IExamineRequirement requirements;

		public void Start()
		{
			// Populate requirements for this item to be examined.
			requirements = new ReqPermitExamine(gameObject);
			requirements = new ReqMaxRange(requirements, MaxDistance);
			requirements = new ReqObstacleCheck(requirements);
		}

		public IExamineRequirement GetRequirements()
		{
			return requirements;
		}

		public IExamineData GetData()
		{
			return new DataNameDescription(DisplayName, Text);
		}
		
    }
}