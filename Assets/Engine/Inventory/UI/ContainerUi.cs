using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


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
        public Text containerName;

        public void Close()
        {
            Inventory.CmdContainerClose(attachedContainer);
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

            // Set the text inside the containerUI to be the name of the container
            containerName.text = attachedContainer.GetName();

            // Position the text correctly inside the UI.
            Vector3[] v = new Vector3[4];
            rect.GetLocalCorners(v); 
            containerName.transform.localPosition = v[1] + new Vector3(0.03f * width, -0.02f * height, 0);

            Item item = attachedContainer.GetComponent<Item>();
            if (item != null)
            {   
                item.ItemContainerChanged += OnItemContainerChanged;
            }
        }

        public void OnItemContainerChanged(Container oldContainer, Container newContainer)
        {
            if (oldContainer != null && newContainer != null)
            {
                //If the item is held in hand, the UI closes when the item is put inside another container.
                //The UI doesn't close if the item is not put in a container, e.g. left on the ground.
                if (Inventory.IsAttachedToHands(oldContainer.AttachedTo) && !(Inventory.IsAttachedToHands(newContainer.AttachedTo)))
                {
                    Close();
                    return;
                }
            }
        }
    }
}