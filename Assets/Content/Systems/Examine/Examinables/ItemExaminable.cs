using SS3D.Engine.Inventory;
using UnityEngine;
using SS3D.Engine.Examine;

namespace SS3D.Content.Systems.Examine.Examinables
{
    [RequireComponent(typeof(Item))]
    public class ItemExaminable : MonoBehaviour, IExaminable
    {
        private Item item;
		private IExamineRequirement requirements;
        [TextArea(1, 15)]
		[SerializeField]
        public string DisplayName;
        [TextArea(5, 15)]
        public string Text;

        public float MaxDistance;
		
        public void Start()
        {
            item = GetComponent<Item>();
			
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
			return new DataNameDescription(item.Name, Text);
		}		
		
    }
}