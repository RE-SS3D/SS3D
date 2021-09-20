﻿using System;
using UnityEngine;
using UnityEngine.UI;
using SS3D.Content.Systems.Substances;
using SS3D.Engine.Substances;

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
            get => attachedContainer;
        }

        private AttachedContainer attachedContainer;
        public Text containerName;
        public Text substanceDisplay;

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

            container.AttachedTo.containerDescriptor.containerUi = this;

            Vector2Int size = container.Size;
            var rectTransform = Grid.GetComponent<RectTransform>();
            Vector2 gridDimensions = Grid.GetGridDimensions();
            float width = rectTransform.offsetMin.x + Math.Abs(rectTransform.offsetMax.x) + gridDimensions.x + 1;
            float height = rectTransform.offsetMin.y + Math.Abs(rectTransform.offsetMax.y) + gridDimensions.y;
            float gridHeight = height;
            if (AttachedContainer.containerDescriptor.canHoldSubstances)
            {
                height += AttachedContainer.containerDescriptor.substanceContainer.Substances.Count * 40;
            }
            var rect = transform.GetChild(0).GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            // Set the text inside the containerUI to be the name of the container
            containerName.text = attachedContainer.GetName();
            

            // Position the text correctly inside the UI.
            Vector3[] v = new Vector3[4];
            rect.GetLocalCorners(v); 
            containerName.transform.localPosition = v[1] + new Vector3(0.03f * width, -0.02f * height, 0);

            DisplaySubstances();
            substanceDisplay.transform.localPosition = v[1] + new Vector3(0.04f * width, -gridHeight - 0.02f * height, 0); ;
            

        }

        private void DisplaySubstances()
        {
            if (AttachedContainer.containerDescriptor.canHoldSubstances)
            {
                substanceDisplay.text = "";
                foreach (SubstanceEntry substance in AttachedContainer.containerDescriptor.substanceContainer.Substances)
                {
                    substanceDisplay.text += substance.Substance.name + ": " + substance.Moles * substance.Substance.MillilitersPerMole;
                }
            }
            else
            {
                substanceDisplay.text = "";
            }
        }
    }
}