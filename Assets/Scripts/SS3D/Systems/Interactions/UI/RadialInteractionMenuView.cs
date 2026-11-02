using System;
using System.Collections.Generic;
using DG.Tweening;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.UI.Shell;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell.Animation;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions.UI
{
    /// <summary>
    /// UI Toolkit view for the radial interaction menu.
    /// </summary>
    public sealed class RadialInteractionMenuView : IUiSurface
    {
        public const float MenuDiameter = 240f;
        private const float PetalSize = 52f;
        private const float PetalOrbitFactor = 0.56f;
        private const float ShowDuration = 0.2f;

        private readonly StyleSheet _menuStyleSheet;
        private readonly Sprite _missingIcon;
        private readonly Sprite _closeIcon;
        private readonly int _maxPetals;

        private readonly PanelAnimator _animator = new(new PanelAnimatorConfig
        {
            Duration = ShowDuration,
            OpenScaleFrom = 0f,
            OpenTranslateYFrom = 0f,
            Ease = Ease.OutCirc,
        });

        private readonly List<RadialInteractionPetal> _petalPool = new();

        private VisualElement _overlayRoot;
        private VisualElement _menuRoot;
        private VisualElement _petalLayer;
        private VisualElement _closeButton;

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

        public void Attach(VisualElement layerRoot)
        {
            _overlayRoot = new VisualElement { name = "radial-interaction-menu-surface" };
            _overlayRoot.style.position = Position.Absolute;
            _overlayRoot.style.left = 0;
            _overlayRoot.style.top = 0;
            _overlayRoot.style.right = 0;
            _overlayRoot.style.bottom = 0;
            _overlayRoot.pickingMode = PickingMode.Ignore;

            if (_menuStyleSheet != null)
            {
                _overlayRoot.styleSheets.Add(_menuStyleSheet);
            }

            layerRoot.Add(_overlayRoot);

            BuildMenuTree();
            SetOverlayInteractive(false);
        }

        public void Detach()
        {
            _animator.CancelImmediate();
            _menuRoot = null;
            _petalLayer = null;
            _closeButton = null;
            _petalPool.Clear();
            _overlayRoot?.RemoveFromHierarchy();
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
            _animator.PlayOpen(_menuRoot);
        }

        public void Hide(Action onComplete = null)
        {
            if (_menuRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            _animator.PlayClose(_menuRoot, onComplete: () =>
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
            if (_overlayRoot.panel == null)
                return;

            Vector2 point = _overlayRoot.WorldToLocal(InputInterface.ScreenToPanel(_overlayRoot.panel, screenPosition));
            float half = MenuDiameter * 0.5f;
            _menuRoot.style.left = point.x - half;
            _menuRoot.style.top = point.y - half;
            _menuRoot.style.bottom = StyleKeyword.Auto;
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
