using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Engine.Examine
{
    [RequireComponent(typeof(Item))]
    public class ItemExaminable : SimpleExaminable
    {
        private Item item;

        private void Start()
        {
            item = GetComponent<Item>();
        }

        public override string GetName(GameObject examinator)
        {
			return item.Name;
        }
		
        public override string GetDescription(GameObject examinator)
        {
            return base.GetDescription(examinator);
        }
		
		public IExamineData GetData()
		{
			return new DataNameDescription(item.Name, "... Description stub");  // FIX THIS **********************
		}		
		
    }
}