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

        public override string GetName()
        {
			return item.Name;
        }
		
        public override string GetDescription()
        {
            return base.GetDescription();
        }
		
		public override IExamineData GetData()
		{
			return new DataNameDescription(item.Name, base.GetDescription());  // FIX THIS **********************
		}		
		
    }
}