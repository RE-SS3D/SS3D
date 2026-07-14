using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Build-tab UI for toggling local tile layer group visibility.
    /// </summary>
    public sealed class TileLayerVisibilityPanel : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _toggleContainer;

        private readonly List<Toggle> _toggles = new();
        private bool _built;

        public void Show()
        {
            EnsureBuilt();
            gameObject.SetActive(true);
            SyncTogglesFromService();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ResetToDefaults()
        {
            TileLayerVisibilityService.ResetGroupDefaults();
            SyncTogglesFromService();
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;

            if (_toggleContainer == null)
                _toggleContainer = GetComponent<RectTransform>();

            VerticalLayoutGroup layout = _toggleContainer.gameObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = _toggleContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            GameObject header = CreateLabel(_toggleContainer, "Layer visibility", 14, FontStyles.Bold);
            header.name = "LayerVisibilityHeader";

            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
            {
                Toggle toggle = CreateCategoryToggle(_toggleContainer, category);
                _toggles.Add(toggle);
            }

            _built = true;
        }

        private void SyncTogglesFromService()
        {
            for (int i = 0; i < _toggles.Count; i++)
            {
                TileLayerCategory category = TileLayerCategoryMapping.AllCategories[i];
                Toggle toggle = _toggles[i];
                bool visible = TileLayerVisibilityService.IsGroupVisible(category);
                toggle.SetIsOnWithoutNotify(visible);
            }
        }

        private static Toggle CreateCategoryToggle(RectTransform parent, TileLayerCategory category)
        {
            GameObject row = new GameObject($"{category}ToggleRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 24f);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform));
            toggleObject.transform.SetParent(row.transform, false);
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(24f, 24f);

            Toggle toggle = toggleObject.AddComponent<Toggle>();

            GameObject background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(toggleObject.transform, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform));
            checkmark.transform.SetParent(background.transform, false);
            Image checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = new Color(0.35f, 0.75f, 1f, 1f);
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = true;

            CreateLabel(row.GetComponent<RectTransform>(), TileLayerCategoryMapping.GetDisplayName(category), 13, FontStyles.Normal);

            toggle.onValueChanged.AddListener(visible => TileLayerVisibilityService.SetGroupVisible(category, visible));
            return toggle;
        }

        private static GameObject CreateLabel(RectTransform parent, string text, int fontSize, FontStyles fontStyle)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement layoutElement = labelObject.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = 24f;

            return labelObject;
        }
    }
}
