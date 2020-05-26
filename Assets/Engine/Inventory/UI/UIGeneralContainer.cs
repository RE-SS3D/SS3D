using System;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Engine.Interactions.UI;

namespace SS3D.Engine.Inventory.UI
{
    /**
    * This renders a container of variable length in a generic fashion
    */
    public class UIGeneralContainer : UIAbstractContainer
    {
        [SerializeField] private GameObject slotPrefab = null;

        public bool Show { get; set; }
        public override bool Highlighted { get; set; } // TODO:

        protected override void RenderContainers(GameObject owner, List<Container> newContainers)
        {
            // Just completely remakes the slot list, as thats easiest to code.
            // TODO: Probably add a few more efficiencies in the future
            slots.Clear();

            int slotStartIndex = 0;
            foreach(var container in newContainers)
            {
                for(int i = 0; i < container.Length(); ++i)
                {
                    var itemSlotObject = Instantiate(slotPrefab, transform);
                    itemSlotObject.transform.localPosition = GetPositionFromIndex(slotStartIndex + i);
                    var slot = itemSlotObject.GetComponent<UIItemSlot>();

                    slots.Add(new SlotInfo(container, i, slot));
                    slot.Item = container.GetItem(i);
                    slot.slotInteractor = this;
                }
                slotStartIndex += container.Length();
            }

            rectTransform.sizeDelta = new Vector2(Math.Min(slotStartIndex, 4) * 30f + 30f, Mathf.Ceil(slotStartIndex / 4f) * 30f + 2f);
        }

        /**
     * Called when any single container updates
     */
        protected override void RenderContainer(Container container) {
            // Note: This assumes container size doesn't change

            int slotIndex = slots.FindIndex(slot => slot.container == container);

            foreach (var item in container.GetItems())
            {
                if(slots[slotIndex].container != container)
                    Debug.LogError("UIGeneralContainer.UpdateContainer can not yet handle container size changing.");

                slots[slotIndex].uiSlot.Item = item;

                slotIndex++;
            }

            if(slots.Count > slotIndex && slots[slotIndex].container == container)
                Debug.LogError("UIGeneralContainer.UpdateContainer was not meant to handle container size changing.");
        }

        private Vector2 GetPositionFromIndex(int i)
        {
            return new Vector2((i % 4) * 30f + 30f, (i / 4) * 30f + 2f);
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private RectTransform rectTransform;
    }
}
