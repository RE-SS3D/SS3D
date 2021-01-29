using System;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SS3D.Engine.Inventory.UI
{
    public class HandsUi : MonoBehaviour
    {
        public GameObject LeftHandPrefab;
        public GameObject RightHandPrefab;
        public Transform HandsContainer;
        public Color SelectedColor;
        /// <summary>
        /// The hands this ui displays
        /// </summary>
        [NonSerialized]
        public Hands Hands;

        private Color defaultColor;
        private int currentHandIndex = -1;

        public void Start()
        {
            Assert.IsNotNull(Hands);
            CreateHandDisplays();
            Hands.HandChanged += OnHandChanged;
            OnHandChanged(Hands.SelectedHandIndex);
        }

        private void OnHandChanged(int index)
        {
            if (currentHandIndex == index)
            {
                return;
            }

            if (currentHandIndex != -1)
            {
                SetHandHighlight(currentHandIndex, false);
            }
            
            SetHandHighlight(index, true);
            currentHandIndex = index;
        }

        private void SetHandHighlight(int index, bool highlight)
        {
            Transform child = HandsContainer.transform.GetChild(index);
            var button = child.GetComponent<Button>();
            ColorBlock buttonColors = button.colors;
            if (highlight)
            {
                defaultColor = buttonColors.normalColor;
                buttonColors.normalColor = SelectedColor;
            }
            else
            {
                buttonColors.normalColor = defaultColor;
            }

            button.colors = buttonColors;
        }

        private void CreateHandDisplays()
        {
            // Destroy existing elements
            Transform containerTransform = HandsContainer.transform;
            int childCount = containerTransform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                DestroyImmediate(containerTransform.GetChild(0).gameObject);
            }

            // Create hand for every hand container
            var containers = Hands.HandContainers;
            for (var i = 0; i < containers.Length; i++)
            {
                AttachedContainer attachedContainer = containers[i];
                GameObject handElement = Instantiate(i % 2 == 0 ? LeftHandPrefab : RightHandPrefab, HandsContainer, false);
                var slot = handElement.GetComponent<SingleItemContainerSlot>();
                slot.Inventory = Hands.Inventory;
                slot.Container = attachedContainer;
            }
        }
    }
}