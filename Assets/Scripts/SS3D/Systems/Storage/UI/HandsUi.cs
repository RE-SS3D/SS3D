using System;
using SS3D.Systems.Storage.Containers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SS3D.Systems.Storage.UI
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

        private Color _defaultColor;
        private int _currentHandIndex = -1;

        public void Start()
        {
            Assert.IsNotNull(Hands);
            CreateHandDisplays();
            Hands.OnHandChanged += OnHandChanged;
            OnHandChanged(Hands.SelectedHandIndex);
        }

        private void OnHandChanged(int index)
        {
            if (_currentHandIndex == index)
            {
                return;
            }

            if (_currentHandIndex != -1)
            {
                SetHandHighlight(_currentHandIndex, false);
            }
            
            SetHandHighlight(index, true);
            _currentHandIndex = index;
        }

        private void SetHandHighlight(int index, bool highlight)
        {
            Transform child = HandsContainer.transform.GetChild(index);
            Button button = child.GetComponent<Button>();
            ColorBlock buttonColors = button.colors;
            if (highlight)
            {
                _defaultColor = buttonColors.normalColor;
                buttonColors.normalColor = SelectedColor;
                buttonColors.highlightedColor = SelectedColor; // The selected hand keeps the same color, highlighted or not.
            }
            else
            {
                buttonColors.normalColor = _defaultColor;
            }

            button.colors = buttonColors;
        }

        private void CreateHandDisplays()
        {
            // Destroy existing elements
            Transform containerTransform = HandsContainer.transform;
            int childCount = containerTransform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                DestroyImmediate(containerTransform.GetChild(0).gameObject);
            }

            // Create hand for every hand container
            AttachedContainer[] containers = Hands.HandContainers;
            for (int i = 0; i < containers.Length; i++)
            {
                AttachedContainer attachedContainer = containers[i];
                GameObject handElement = Instantiate(i % 2 == 0 ? LeftHandPrefab : RightHandPrefab, HandsContainer, false);
                SingleItemContainerSlot slot = handElement.GetComponent<SingleItemContainerSlot>();
                slot.Inventory = Hands.Inventory;
                slot.Container = attachedContainer;
            }
        }
    }
}