using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions.UI
{
    /// <summary>
    /// UI Toolkit overlay for armed Tier 2/3 interactions (reticle, action chip, invalid feedback).
    /// </summary>
    public sealed class ArmedInteractionOverlayView
    {
        private const float ReticleSize = 26f;
        private const float ChipOffsetY = 24f;

        private readonly StyleSheet _styleSheet;

        private VisualElement _overlayRoot;
        private VisualElement _reticle;
        private VisualElement _chip;
        private Label _chipLabel;
        private Label _chipHint;
        private Label _cantBadge;

        public ArmedInteractionOverlayView(StyleSheet styleSheet)
        {
            _styleSheet = styleSheet;
        }

        public void Attach(VisualElement overlayRoot)
        {
            _overlayRoot = overlayRoot;
            _overlayRoot.style.flexGrow = 1;
            _overlayRoot.pickingMode = PickingMode.Ignore;

            if (_styleSheet != null)
            {
                _overlayRoot.styleSheets.Add(_styleSheet);
            }

            BuildOverlayTree();
            SetVisible(false);
        }

        public void Detach()
        {
            _overlayRoot = null;
            _reticle = null;
            _chip = null;
            _chipLabel = null;
            _chipHint = null;
            _cantBadge = null;
        }

        public void Show(string chipLabel)
        {
            _chipLabel.text = chipLabel;
            SetVisible(true);
            UpdateCursorPosition(Mouse.current.position.ReadValue());
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void UpdateCursorPosition(Vector2 screenPosition)
        {
            if (_reticle == null)
            {
                return;
            }

            float half = ReticleSize * 0.5f;
            _reticle.style.left = screenPosition.x - half;
            _reticle.style.bottom = screenPosition.y - half;

            _chip.style.left = screenPosition.x;
            _chip.style.bottom = screenPosition.y - ChipOffsetY;
            _chip.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);

            _cantBadge.style.left = screenPosition.x + 18f;
            _cantBadge.style.bottom = screenPosition.y + 10f;
        }

        public void SetTargetState(bool hasTarget, bool isValid)
        {
            _reticle.EnableInClassList("armed-interaction-reticle--valid", hasTarget && isValid);
            _reticle.EnableInClassList("armed-interaction-reticle--invalid", hasTarget && !isValid);
            _cantBadge.style.display = hasTarget && !isValid ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void BuildOverlayTree()
        {
            VisualElement container = new();
            container.AddToClassList("armed-interaction-overlay");
            container.pickingMode = PickingMode.Ignore;

            _reticle = new VisualElement();
            _reticle.AddToClassList("armed-interaction-reticle");
            _reticle.pickingMode = PickingMode.Ignore;

            VisualElement ring = new();
            ring.AddToClassList("armed-interaction-reticle__ring");
            ring.pickingMode = PickingMode.Ignore;

            foreach (string tickClass in new[]
                     {
                         "armed-interaction-reticle__tick--top",
                         "armed-interaction-reticle__tick--bottom",
                         "armed-interaction-reticle__tick--left",
                         "armed-interaction-reticle__tick--right",
                     })
            {
                VisualElement tick = new();
                tick.AddToClassList("armed-interaction-reticle__tick");
                tick.AddToClassList(tickClass);
                tick.pickingMode = PickingMode.Ignore;
                _reticle.Add(tick);
            }

            _reticle.Add(ring);

            _chip = new VisualElement();
            _chip.AddToClassList("armed-interaction-chip");
            _chip.pickingMode = PickingMode.Ignore;

            _chipLabel = new Label();
            _chipLabel.AddToClassList("armed-interaction-chip__label");
            _chipLabel.AddToClassList("font-terminal");
            _chipLabel.pickingMode = PickingMode.Ignore;

            _chipHint = new Label("Esc or right-click to cancel");
            _chipHint.AddToClassList("armed-interaction-chip__hint");
            _chipHint.pickingMode = PickingMode.Ignore;

            _chip.Add(_chipLabel);
            _chip.Add(_chipHint);

            _cantBadge = new Label("CAN'T");
            _cantBadge.AddToClassList("armed-interaction-cant");
            _cantBadge.AddToClassList("font-arcade");
            _cantBadge.pickingMode = PickingMode.Ignore;
            _cantBadge.style.display = DisplayStyle.None;

            container.Add(_reticle);
            container.Add(_chip);
            container.Add(_cantBadge);
            _overlayRoot.Add(container);
        }

        private void SetVisible(bool visible)
        {
            if (_overlayRoot == null || _overlayRoot.childCount == 0)
            {
                return;
            }

            _overlayRoot[0].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
