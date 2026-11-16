using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Examine
{
    /// <summary>
    /// Cursor-anchored hover/detailed examine UI for every non-character examinable — replaces the
    /// condemned uGUI <c>ExamineUI</c>/<c>ExamineDetailedView</c>/<c>ExamineImageDetailedView</c> trio.
    /// Plain hover shows a name-only label; Shift-held (or a radial-menu pin) shows a detailed text or
    /// image panel instead, matching the deleted uGUI system's exact behavior split.
    /// </summary>
    public sealed class GenericExamineHoverView : IUiSurface
    {
        private const float OffsetX = 16f;
        private const float OffsetY = 16f;

        private readonly StyleSheet _examineStyle;

        private VisualElement _root;
        private Label _hoverLabel;
        private VisualElement _textPanel;
        private Label _textName;
        private Label _textDescription;
        private VisualElement _imagePanel;
        private Image _image;
        private Label _imageCaption;
        private VisualElement _activePanel;

        public GenericExamineHoverView(StyleSheet examineStyle)
        {
            _examineStyle = examineStyle;
        }

        public void Attach(VisualElement layerRoot)
        {
            _root = new VisualElement { name = "generic-examine-hover-surface" };
            _root.style.flexGrow = 1;
            _root.pickingMode = PickingMode.Ignore;

            if (_examineStyle != null)
            {
                _root.styleSheets.Add(_examineStyle);
            }

            layerRoot.Add(_root);

            BuildTree();
            HideAll();
        }

        public void Detach()
        {
            _hoverLabel = null;
            _textPanel = null;
            _textName = null;
            _textDescription = null;
            _imagePanel = null;
            _image = null;
            _imageCaption = null;
            _activePanel = null;
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        public void ShowHoverName(string name)
        {
            if (_hoverLabel == null || string.IsNullOrEmpty(name))
            {
                HideAll();
                return;
            }

            HideAll();
            _hoverLabel.text = name;
            SetActivePanel(_hoverLabel);
        }

        public void ShowDetailedText(string name, string description)
        {
            if (_textPanel == null)
            {
                return;
            }

            HideAll();
            _textName.text = name;
            _textName.style.display = string.IsNullOrEmpty(name) ? DisplayStyle.None : DisplayStyle.Flex;
            _textDescription.text = description;
            _textDescription.style.display = string.IsNullOrEmpty(description) ? DisplayStyle.None : DisplayStyle.Flex;
            SetActivePanel(_textPanel);
        }

        public void ShowDetailedImage(Sprite image, string caption, Vector2 imageSize)
        {
            if (_imagePanel == null)
            {
                return;
            }

            HideAll();
            _image.sprite = image;
            if (imageSize != Vector2.zero)
            {
                _image.style.width = imageSize.x;
                _image.style.height = imageSize.y;
            }

            _imageCaption.text = caption;
            _imageCaption.style.display = string.IsNullOrEmpty(caption) ? DisplayStyle.None : DisplayStyle.Flex;
            SetActivePanel(_imagePanel);
        }

        public void Hide()
        {
            HideAll();
        }

        /// <param name="screenPosition">Bottom-left screen pixels (mouse / Input System).</param>
        public void UpdateAnchor(Vector2 screenPosition)
        {
            if (_activePanel == null)
            {
                return;
            }

            // Match ZoneTargetReticle: mouse is bottom-left screen; UITK absolute layout is panel
            // top-left. Do not clamp against Screen/root sizes — mismatched spaces pinned Y in the
            // lower half of the screen (horizontal still tracked).
            Vector2 panelPos = screenPosition;
            IPanel panel = _activePanel.panel;
            if (panel != null)
            {
                panelPos = InputInterface.ScreenToPanel(panel, screenPosition);
            }

            _activePanel.style.left = panelPos.x + OffsetX;
            _activePanel.style.top = panelPos.y + OffsetY;
            _activePanel.style.right = StyleKeyword.Auto;
            _activePanel.style.bottom = StyleKeyword.Auto;
        }

        private void SetActivePanel(VisualElement panel)
        {
            _activePanel = panel;
            panel.style.display = DisplayStyle.Flex;
        }

        private void HideAll()
        {
            _activePanel = null;
            if (_hoverLabel != null)
            {
                _hoverLabel.style.display = DisplayStyle.None;
            }

            if (_textPanel != null)
            {
                _textPanel.style.display = DisplayStyle.None;
            }

            if (_imagePanel != null)
            {
                _imagePanel.style.display = DisplayStyle.None;
            }
        }

        private void BuildTree()
        {
            _hoverLabel = new Label();
            _hoverLabel.AddToClassList("examine-hover-name");
            _hoverLabel.AddToClassList("font-body");
            _hoverLabel.pickingMode = PickingMode.Ignore;

            _textPanel = new VisualElement();
            _textPanel.AddToClassList("examine-detail-text");
            _textPanel.pickingMode = PickingMode.Ignore;

            _textName = new Label();
            _textName.AddToClassList("examine-detail-text__name");
            _textName.AddToClassList("font-titling");
            _textName.pickingMode = PickingMode.Ignore;

            _textDescription = new Label();
            _textDescription.AddToClassList("examine-detail-text__description");
            _textDescription.AddToClassList("font-body");
            _textDescription.pickingMode = PickingMode.Ignore;

            _textPanel.Add(_textName);
            _textPanel.Add(_textDescription);

            _imagePanel = new VisualElement();
            _imagePanel.AddToClassList("examine-detail-image");
            _imagePanel.pickingMode = PickingMode.Ignore;

            _image = new Image();
            _image.AddToClassList("examine-detail-image__image");
            _image.scaleMode = ScaleMode.ScaleToFit;
            _image.pickingMode = PickingMode.Ignore;

            _imageCaption = new Label();
            _imageCaption.AddToClassList("examine-detail-image__caption");
            _imageCaption.AddToClassList("font-body");
            _imageCaption.pickingMode = PickingMode.Ignore;

            _imagePanel.Add(_image);
            _imagePanel.Add(_imageCaption);

            _root.Add(_hoverLabel);
            _root.Add(_textPanel);
            _root.Add(_imagePanel);
        }
    }
}
