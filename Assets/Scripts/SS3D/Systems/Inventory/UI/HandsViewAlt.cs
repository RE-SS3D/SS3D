using System;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// This script instantiate the hand UI prefabs, and set up the SingleItemContainerSlot on each hand,
    /// such that the slots are displaying the right containers. It also handles setting the highlight of the active hand.
    /// </summary>
    public class HandsViewAlt : View
    {

        public Transform HandsContainer;
        public Color SelectedColor;

        /// <summary>
        /// The hands this ui displays
        /// </summary>
        [NonSerialized]
        public HandsAlt Hands;

        private Color _defaultColor;
        private int _currentHandIndex = -1;

        protected override void OnStart()
        {
            base.OnStart();

            Assert.IsNotNull(Hands);
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
    }
}