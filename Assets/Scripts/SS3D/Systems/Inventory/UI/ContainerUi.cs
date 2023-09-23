using System;
using Coimbra;
using SS3D.Systems.Inventory.Containers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    public class ContainerUi : MonoBehaviour
    {
        public ItemGrid Grid;
        public TMP_Text ContainerName;

        private AttachedContainer _attachedContainer;

        public HumanInventory Inventory
        {
            get => Grid.Inventory;
            set => Grid.Inventory = value;
        }

        public AttachedContainer AttachedContainer
        {
            set
            {
                _attachedContainer = value;
                Grid.AttachedContainer = value;
                UpdateContainer(value);
            }
        }

        public void Close()
        {
            Inventory.ContainerViewer.CmdContainerClose(_attachedContainer);
            gameObject.Dispose(true);
        }

        private void UpdateContainer(AttachedContainer container)
        {
            if (container == null)
            {
                return;
            }

            container.ContainerUi = this;

            RectTransform rectTransform = Grid.GetComponent<RectTransform>();
            Vector2 gridDimensions = Grid.GetGridDimensions();
            float width = rectTransform.offsetMin.x + Math.Abs(rectTransform.offsetMax.x) + gridDimensions.x + 1;
            float height = rectTransform.offsetMin.y + Math.Abs(rectTransform.offsetMax.y) + gridDimensions.y;
            RectTransform rect = transform.GetChild(0).GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            // Set the text inside the containerUI to be the name of the container
            ContainerName.text = _attachedContainer.ContainerName;

            // Position the text correctly inside the UI.
            Vector3[] fourCornersArray = new Vector3[4];
            rect.GetLocalCorners(fourCornersArray);
            ContainerName.transform.localPosition = fourCornersArray[1] + new Vector3(0.03f * width, -0.02f * height, 0);
        }
    }
}