using System;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Inventory.UI
{
    public class ContainerUi : MonoBehaviour
    {
        public ItemGrid Grid;

        public Inventory Inventory
        {
            set => Grid.Inventory = value;
            get => Grid.Inventory;
        }

        public AttachedContainer AttachedContainer
        {
            set
            {
                attachedContainer = value;
                Grid.AttachedContainer = value;
                UpdateContainer(value.Container);
            }
        }

        private AttachedContainer attachedContainer;

        public void Close()
        {
            Inventory.CmdContainerClose(attachedContainer);
            Destroy(gameObject);
        }

        private void UpdateContainer(Container container)
        {
            if (container == null)
            {
                return;
            }

            Vector2Int size = container.Size;
            var rectTransform = Grid.GetComponent<RectTransform>();
            Vector2 gridDimensions = Grid.GetGridDimensions();
            float width = rectTransform.offsetMin.x + Math.Abs(rectTransform.offsetMax.x) + gridDimensions.x + 1;
            float height = rectTransform.offsetMin.y + Math.Abs(rectTransform.offsetMax.y) + gridDimensions.y;
            var rect = transform.GetChild(0).GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}