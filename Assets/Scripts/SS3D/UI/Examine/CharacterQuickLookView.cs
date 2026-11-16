using System.Collections.Generic;
using SS3D.Systems.Examine;
using SS3D.Systems.Inputs;
using SS3D.UI.Examine.Components;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Examine
{
    /// <summary>
    /// Compact hover "quick-look" preview for character examine — a name header over a small
    /// paperdoll grid, anchored near the cursor while an <see cref="ExamineType.CHARACTER"/>-tagged
    /// examinable is hovered. Unlike every other examinable, this shows on plain hover with no
    /// Shift/hold-to-peek gate — see
    /// the Deviations list in Documents/architecture/2026-11_examine-and-action-feedback.md for why character examine's
    /// hover/Shift+Click model departs from the general hold-to-peek rule.
    /// </summary>
    public sealed class CharacterQuickLookView : IUiSurface
    {
        private const float OffsetX = 24f;
        /// <summary>
        /// Gap above the cursor before the panel's bottom edge. Combined with a -100% Y translate
        /// so the paperdoll sits above the pointer (same visual as the old style.bottom anchor).
        /// </summary>
        private const float OffsetY = -16f;
        private const float SlotSize = 40f;

        private readonly StyleSheet _examineStyle;
        private readonly StyleSheet _inventorySlotStyle;
        private readonly StyleSheet _diegeticTokensStyle;
        private readonly CharacterExamineIconSet _icons;

        private VisualElement _root;
        private VisualElement _panel;
        private Label _nameLabel;
        private CharacterPaperdollGrid _grid;

        public CharacterQuickLookView(
            StyleSheet examineStyle,
            StyleSheet inventorySlotStyle,
            StyleSheet diegeticTokensStyle,
            CharacterExamineIconSet icons)
        {
            _examineStyle = examineStyle;
            _inventorySlotStyle = inventorySlotStyle;
            _diegeticTokensStyle = diegeticTokensStyle;
            _icons = icons;
        }

        public void Attach(VisualElement layerRoot)
        {
            _root = new VisualElement { name = "character-quicklook-surface" };
            _root.style.flexGrow = 1;
            _root.pickingMode = PickingMode.Ignore;

            if (_examineStyle != null)
            {
                _root.styleSheets.Add(_examineStyle);
            }

            if (_inventorySlotStyle != null)
            {
                _root.styleSheets.Add(_inventorySlotStyle);
            }

            if (_diegeticTokensStyle != null)
            {
                _root.styleSheets.Add(_diegeticTokensStyle);
            }

            layerRoot.Add(_root);

            BuildTree();
            SetVisible(false);
        }

        public void Detach()
        {
            _panel = null;
            _nameLabel = null;
            _grid = null;
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        public void Show(string playerName, IReadOnlyList<CharacterExamineSlotContent> slots)
        {
            if (_panel == null)
            {
                return;
            }

            _nameLabel.text = playerName;
            foreach (CharacterExamineSlotContent content in slots)
            {
                _grid.SetSlot(content);
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        /// <param name="screenPosition">Bottom-left screen pixels (mouse / Input System).</param>
        public void UpdateAnchor(Vector2 screenPosition)
        {
            if (_panel == null)
            {
                return;
            }

            Vector2 panelPos = screenPosition;
            IPanel panel = _panel.panel;
            if (panel != null)
            {
                panelPos = InputInterface.ScreenToPanel(panel, screenPosition);
            }

            // Top-left panel coords + -100% Y translate: bottom of the paperdoll sits near the
            // cursor (old style.bottom + mouse.y placed the panel above the pointer).
            _panel.style.left = panelPos.x + OffsetX;
            _panel.style.top = panelPos.y + OffsetY;
            _panel.style.right = StyleKeyword.Auto;
            _panel.style.bottom = StyleKeyword.Auto;
            _panel.style.translate = new Translate(0, new Length(-100, LengthUnit.Percent));
        }

        private void BuildTree()
        {
            _panel = new VisualElement();
            _panel.AddToClassList("character-quicklook");
            _panel.pickingMode = PickingMode.Ignore;

            VisualElement clip = new();
            clip.AddToClassList("character-quicklook__clip");
            clip.pickingMode = PickingMode.Ignore;

            _nameLabel = new Label();
            _nameLabel.AddToClassList("character-quicklook__name");
            _nameLabel.AddToClassList("font-titling");
            _nameLabel.pickingMode = PickingMode.Ignore;

            _grid = new CharacterPaperdollGrid(SlotSize, _icons);
            _grid.pickingMode = PickingMode.Ignore;
            _grid.AddToClassList("character-quicklook__grid");

            clip.Add(_nameLabel);
            clip.Add(_grid);
            _panel.Add(clip);
            _root.Add(_panel);
        }

        private void SetVisible(bool visible)
        {
            if (_panel == null)
            {
                return;
            }

            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
