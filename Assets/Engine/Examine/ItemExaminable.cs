using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Examine
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
			requirements = new ReqPermitExamine(gameObject);
			requirements = new ReqMaxRange(requirements, MaxDistance);
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