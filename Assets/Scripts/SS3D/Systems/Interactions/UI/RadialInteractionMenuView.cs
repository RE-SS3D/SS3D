using System;
using System.Collections.Generic;
using DG.Tweening;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions.UI
{
    /// <summary>
    /// UI Toolkit view for the radial interaction menu.
    /// </summary>
    public sealed class RadialInteractionMenuView
    {
        public const float MenuDiameter = 240f;
        private const float PetalSize = 52f;
        private const float PetalOrbitFactor = 0.56f;
        private const float ShowDuration = 0.2f;

        private readonly StyleSheet _menuStyleSheet;
        private readonly Sprite _missingIcon;
        private readonly Sprite _closeIcon;
        private readonly int _maxPetals;

        private readonly List<RadialInteractionPetal> _petalPool = new();

        private VisualElement _overlayRoot;
        private VisualElement _menuRoot;
        private VisualElement _petalLayer;
        private VisualElement _closeButton;

        private Sequence _showSequence;

        public RadialInteractionMenuView(StyleSheet menuStyleSheet, Sprite missingIcon, Sprite closeIcon, int maxPetals = 12)
        {
            _menuStyleSheet = menuStyleSheet;
            _missingIcon = missingIcon;
            _closeIcon = closeIcon;
            _maxPetals = maxPetals;
        }

        public event Action CloseRequested;
        public event Action<IInteraction> InteractionSelected;

        public float MenuHeight => MenuDiameter;

        public void Attach(VisualElement overlayRoot)
        {
            _overlayRoot = overlayRoot;
            _overlayRoot.style.flexGrow = 1;
            _overlayRoot.pickingMode = PickingMode.Ignore;

            if (_menuStyleSheet != null)
            {
                _overlayRoot.styleSheets.Add(_menuStyleSheet);
            }

            BuildMenuTree();
            SetOverlayInteractive(false);
        }

        public void Detach()
        {
            _showSequence?.Kill();
            _menuRoot?.RemoveFromHierarchy();
            _menuRoot = null;
            _petalLayer = null;
            _closeButton = null;
            _petalPool.Clear();
            _overlayRoot = null;
        }

        public void Show(IReadOnlyList<IInteraction> interactions, InteractionEvent interactionEvent, Vector2 screenPosition)
        {
            if (_menuRoot == null)
            {
                return;
            }

            PopulatePetals(interactions, interactionEvent);
            PositionAt(screenPosition);
            SetOverlayInteractive(true);

            _showSequence?.Kill();
            _menuRoot.style.opacity = 0;
            float scaleValue = 0f;
            _menuRoot.style.scale = new Scale(new Vector3(scaleValue, scaleValue, 1f));

            _showSequence = DOTween.Sequence();
            _showSequence.Append(DOTween.To(() => _menuRoot.style.opacity.value, value => _menuRoot.style.opacity = value, 1f, ShowDuration)
                .SetEase(Ease.OutCirc));
            _showSequence.Join(DOTween.To(() => scaleValue, value =>
            {
                scaleValue = value;
                _menuRoot.style.scale = new Scale(new Vector3(value, value, 1f));
            }, 1f, ShowDuration).SetEase(Ease.OutCirc));
        }

        public void Hide(Action onComplete = null)
        {
            if (_menuRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            _showSequence?.Kill();
            float hideScale = 1f;
            _showSequence = DOTween.Sequence();
            _showSequence.Append(DOTween.To(() => _menuRoot.style.opacity.value, value => _menuRoot.style.opacity = value, 0f, ShowDuration)
                .SetEase(Ease.OutCirc));
            _showSequence.Join(DOTween.To(() => hideScale, value =>
            {
                hideScale = value;
                _menuRoot.style.scale = new Scale(new Vector3(value, value, 1f));
            }, 0f, ShowDuration).SetEase(Ease.OutCirc));
            _showSequence.OnComplete(() =>
            {
                ClearPetals();
                SetOverlayInteractive(false);
                onComplete?.Invoke();
            });
        }

        private void BuildMenuTree()
        {
            _menuRoot = new VisualElement();
            _menuRoot.AddToClassList("radial-interaction-menu");
            _menuRoot.pickingMode = PickingMode.Position;
            _menuRoot.style.display = DisplayStyle.None;

            VisualElement background = new();
            background.AddToClassList("radial-interaction-menu__background");
            background.pickingMode = PickingMode.Ignore;

            _petalLayer = new VisualElement();
            _petalLayer.AddToClassList("radial-interaction-menu__petal-layer");
            _petalLayer.pickingMode = PickingMode.Ignore;

            _closeButton = new VisualElement();
            _closeButton.AddToClassList("radial-interaction-menu__close");
            _closeButton.pickingMode = PickingMode.Position;
            _closeButton.RegisterCallback<ClickEvent>(_ => CloseRequested?.Invoke());

            VisualElement closeIcon = new();
            closeIcon.AddToClassList("radial-interaction-menu__close-icon");
            closeIcon.pickingMode = PickingMode.Ignore;
            if (_closeIcon != null)
            {
                closeIcon.style.backgroundImage = new StyleBackground(_closeIcon);
            }

            _closeButton.Add(closeIcon);
            _menuRoot.Add(background);
            _menuRoot.Add(_petalLayer);
            _menuRoot.Add(_closeButton);
            _overlayRoot.Add(_menuRoot);
        }

        private void PopulatePetals(IReadOnlyList<IInteraction> interactions, InteractionEvent interactionEvent)
        {
            ClearPetals();

            int count = Mathf.Min(interactions.Count, _maxPetals);
            float radius = MenuDiameter * 0.5f;
            float petalOrbit = radius * PetalOrbitFactor;

            for (int i = 0; i < count; i++)
            {
                IInteraction interaction = interactions[i];
                Sprite icon = interaction.GetIcon(interactionEvent);
                if (icon == null)
                {
                    icon = _missingIcon;
                }

                RadialInteractionPetal petal = GetOrCreatePetal(i);
                InteractionTier tier = interaction.GetInteractionTier(interactionEvent);
                string label = interaction.GetName(interactionEvent);

                float angle = (i / (float)count) * Mathf.PI * 2f - Mathf.PI / 2f;
                bool labelAbove = Mathf.Sin(angle) <= -0.4f;
                petal.SetInteraction(interaction, icon, tier, label, labelAbove);

                float x = Mathf.Cos(angle) * petalOrbit;
                float y = Mathf.Sin(angle) * petalOrbit;

                petal.style.left = radius + x - PetalSize * 0.5f;
                petal.style.top = radius + y - PetalSize * 0.5f;
                _petalLayer.Add(petal);
            }
        }

        private RadialInteractionPetal GetOrCreatePetal(int index)
        {
            while (_petalPool.Count <= index)
            {
                RadialInteractionPetal petal = new();
                petal.Clicked += HandlePetalClicked;
                _petalPool.Add(petal);
            }

            return _petalPool[index];
        }

        private void HandlePetalClicked(IInteraction interaction)
        {
            InteractionSelected?.Invoke(interaction);
        }

        private void ClearPetals()
        {
            foreach (RadialInteractionPetal petal in _petalPool)
            {
                petal.ResetPetal();
                petal.RemoveFromHierarchy();
            }
        }

        private void PositionAt(Vector2 screenPosition)
        {
            float half = MenuDiameter * 0.5f;
            _menuRoot.style.left = screenPosition.x - half;
            _menuRoot.style.bottom = screenPosition.y - half;
        }

        private void SetOverlayInteractive(bool interactive)
        {
            if (_overlayRoot == null || _menuRoot == null)
            {
                return;
            }

            _overlayRoot.pickingMode = interactive ? PickingMode.Position : PickingMode.Ignore;
            _menuRoot.style.display = interactive ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
