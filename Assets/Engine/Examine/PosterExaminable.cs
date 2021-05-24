using UnityEngine;

namespace SS3D.Engine.Examine
{
    public class PosterExaminable : MonoBehaviour, IExaminable
    {
	
		public Sprite Picture;
        public string Caption;
		public float MaxDistance = 2;
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
			return new DataImage(Caption, Picture);
		}
		
    }
}