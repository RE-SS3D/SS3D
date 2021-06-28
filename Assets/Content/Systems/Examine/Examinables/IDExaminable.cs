/*

using Mirror;
using UnityEngine;
using SS3D.Engine.Examine;

namespace SS3D.Content.Systems.Examine.Examinables
{
    public class IDExaminable : MonoBehaviour, IExaminable
    {

		public float MaxDistance = 2;
		private IExamineRequirement requirements;
		private DataIdentificationCard IdDetails;

		public void Start()
		{
			// Populate requirements for this item to be examined.
			requirements = new ReqPermitExamine(gameObject);
			requirements = new ReqMaxRange(requirements, MaxDistance);
			requirements = new ReqObstacleCheck(requirements);
				
			// Populate the actual ID details randomly
			IdDetails = new DataIdentificationCard();
				
		
		}		

		public IExamineRequirement GetRequirements()
		{
			return requirements;
		}

		public IExamineData GetData()
		{
			return IdDetails;
		}
		
    }
}

*/