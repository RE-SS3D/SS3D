using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Build-tab UI for toggling local tile layer group visibility via a multi-select dropdown.
    /// </summary>
    public sealed class TileLayerVisibilityPanel : MonoBehaviour
    {
        private TMP_Dropdown _dropdown;
        private bool _built;

        public static TileLayerVisibilityPanel Create(TMP_Dropdown prototype, RectTransform parent, int siblingIndex)
        {
            GameObject dropdownObject = Instantiate(prototype.gameObject, parent);
            dropdownObject.name = "LayerVisibilityDropdown";
            dropdownObject.transform.SetSiblingIndex(siblingIndex);

            RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
            RectTransform prototypeRect = prototype.GetComponent<RectTransform>();
            dropdownRect.anchorMin = prototypeRect.anchorMin;
            dropdownRect.anchorMax = prototypeRect.anchorMax;
            dropdownRect.pivot = prototypeRect.pivot;
            dropdownRect.sizeDelta = prototypeRect.sizeDelta;
            dropdownRect.anchoredPosition = Vector2.zero;

            LayoutElement layoutElement = dropdownObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = dropdownObject.AddComponent<LayoutElement>();

            layoutElement.preferredHeight = prototypeRect.sizeDelta.y > 0f ? prototypeRect.sizeDelta.y : 20f;
            layoutElement.flexibleWidth = 1f;

            TileLayerVisibilityPanel panel = dropdownObject.AddComponent<TileLayerVisibilityPanel>();
            panel.Build(dropdownObject.GetComponent<TMP_Dropdown>());
            return panel;
        }

        public void Show()
        {
            if (_dropdown != null)
                _dropdown.gameObject.SetActive(true);

            SyncDropdownFromService();
        }

        public void Hide()
        {
            if (_dropdown != null)
                _dropdown.gameObject.SetActive(false);
        }

        public void ResetToDefaults()
        {
            TileLayerVisibilityService.ResetGroupDefaults();
            SyncDropdownFromService();
        }

        private void Build(TMP_Dropdown dropdown)
        {
            if (_built)
                return;

            _dropdown = dropdown;
            _dropdown.onValueChanged.RemoveAllListeners();

            _dropdown.options.Clear();
            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
            {
                _dropdown.options.Add(new TMP_Dropdown.OptionData(TileLayerCategoryMapping.GetDisplayName(category)));
            }

            _dropdown.MultiSelect = true;
            _dropdown.value = EverythingValue(_dropdown.options.Count);
            _dropdown.RefreshShownValue();
            _dropdown.onValueChanged.AddListener(HandleDropdownValueChanged);

            _built = true;
        }

        private void SyncDropdownFromService()
        {
            if (_dropdown == null)
                return;

            int value = 0;
            for (int i = 0; i < TileLayerCategoryMapping.AllCategories.Length; i++)
            {
                if (TileLayerVisibilityService.IsGroupVisible(TileLayerCategoryMapping.AllCategories[i]))
                    value |= 1 << i;
            }

            _dropdown.SetValueWithoutNotify(value);
            _dropdown.RefreshShownValue();
        }

        private void HandleDropdownValueChanged(int value)
        {
            for (int i = 0; i < TileLayerCategoryMapping.AllCategories.Length; i++)
            {
                bool visible = (value & (1 << i)) != 0;
                TileLayerVisibilityService.SetGroupVisible(TileLayerCategoryMapping.AllCategories[i], visible);
            }
        }

        private static int EverythingValue(int count)
        {
            int result = 0;
            for (int i = 0; i < count; i++)
                result |= 1 << i;

            return result;
        }
    }
}
