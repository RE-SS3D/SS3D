using Coimbra.Services.Events;
using System;
using System.Collections.Generic;
using DG.Tweening;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Controls the UI for a radial interaction menu
    /// </summary>
    public sealed class RadialInteractionSubSystem : SubSystem
    {
        public event Action<IInteraction, RadialInteractionButton> OnInteractionSelected;

        [Header("UI")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _petalContainer;
        [SerializeField] private Sprite _missingIcon;
        [SerializeField] private Sprite _closeButtonSprite;
        [SerializeField] private Sprite _closeIconSprite;

        [Header("Buttons")]
        [SerializeField] private RadialInteractionButton _petalPrefab;
        [SerializeField] private int _maxPetals = 12;

        private const float MenuDiameter = 176f;
        private const float PetalSize = 48f;
        private const float CenterButtonSize = 36f;
        private const float ScaleDuration = .2f;

        private readonly List<RadialInteractionButton> _petalPool = new();
        private Button _closeButton;

        private Sequence _scaleSequence;
        private Sequence _fadeSequence;

        private List<IInteraction> Interactions { get; set; }
        private InteractionEvent Event { get; set; }
        private Controls.InteractionsActions _controls;
        private InputSubSystem _inputSystem;

        protected override void OnAwake()
        {
            base.OnAwake();

            Setup();
        }

        protected override void OnStart()
        {
            base.OnStart();

            Disappear();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            _controls.ViewInteractions.canceled += HandleDisappear;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            _controls.ViewInteractions.canceled -= HandleDisappear;
        }

        private void Setup()
        {
            RectTransform.sizeDelta = new Vector2(MenuDiameter, MenuDiameter);

            if (_petalContainer == null)
            {
                _petalContainer = RectTransform;
            }

            CreateCloseButton();

            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs.Interactions;
        }

        private void CreateCloseButton()
        {
            GameObject closeObject = new("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(Transform, false);

            RectTransform closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0.5f);
            closeRect.anchorMax = new Vector2(0.5f, 0.5f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(CenterButtonSize, CenterButtonSize);
            closeRect.anchoredPosition = Vector2.zero;

            Image background = closeObject.GetComponent<Image>();
            background.sprite = _closeButtonSprite;
            background.raycastTarget = true;

            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(closeObject.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(16f, 16f);
            iconRect.anchoredPosition = Vector2.zero;

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = _closeIconSprite;
            icon.raycastTarget = false;
            icon.color = new Color(1f, 1f, 1f, 0.6f);

            _closeButton = closeObject.GetComponent<Button>();
            _closeButton.targetGraphic = background;
            _closeButton.onClick.AddListener(HandleClosePressed);
        }

        private void HandleClosePressed()
        {
            _inputSystem.ToggleBinding("<Mouse>/leftButton", true);
            Disappear();
        }

        private void HandleInteractionButtonPressed(IInteraction interaction, RadialInteractionButton radialInteractionButton)
        {
            radialInteractionButton.OnInteractionSelected -= HandleInteractionButtonPressed;

            Disappear();
            OnInteractionSelected?.Invoke(interaction, radialInteractionButton);
        }

        /// <summary>
        /// Opens the interaction menu
        /// </summary>
        public void ShowInteractionsMenu()
        {
            bool hasInteractions = Event != null && !Interactions.IsNullOrEmpty();
            if (!hasInteractions) { return; }

            int index = 0;
            foreach (IInteraction interaction in Interactions)
            {
                if (index >= _maxPetals)
                {
                    break;
                }

                Sprite icon = interaction.GetIcon(Event);
                if (icon == null)
                {
                    icon = _missingIcon;
                }
                string interactionName = interaction.GetName(Event);
                string objectName = Event.Target.ToString();

                RadialInteractionItem radialInteractionItem = new(icon, interactionName, interaction, objectName);

                RadialInteractionButton interactionButton = GetOrCreatePetal(index);
                interactionButton.SetInteraction(radialInteractionItem);
                interactionButton.OnInteractionSelected += HandleInteractionButtonPressed;
                index++;
            }

            LayoutPetals(index);
            Show();
        }

        private RadialInteractionButton GetOrCreatePetal(int index)
        {
            while (_petalPool.Count <= index)
            {
                RadialInteractionButton petal = Instantiate(_petalPrefab, _petalContainer);
                petal.gameObject.SetActive(false);
                _petalPool.Add(petal);
            }

            return _petalPool[index];
        }

        private void LayoutPetals(int count)
        {
            float radius = MenuDiameter * 0.5f;
            float petalOrbit = radius * 0.66f;

            for (int i = 0; i < _petalPool.Count; i++)
            {
                RadialInteractionButton petal = _petalPool[i];
                bool active = i < count;

                if (!active)
                {
                    petal.Reset();
                    continue;
                }

                float angle = (i / (float)count) * Mathf.PI * 2f - Mathf.PI / 2f;
                float x = Mathf.Cos(angle) * petalOrbit;
                float y = Mathf.Sin(angle) * petalOrbit;

                RectTransform petalRect = petal.RectTransform;
                petalRect.anchorMin = new Vector2(0.5f, 0.5f);
                petalRect.anchorMax = new Vector2(0.5f, 0.5f);
                petalRect.pivot = new Vector2(0.5f, 0.5f);
                petalRect.sizeDelta = new Vector2(PetalSize, PetalSize);
                petalRect.anchoredPosition = new Vector2(x, y);
                petalRect.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Tweens the UI on the enabled position
        /// </summary>
        private void Show()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Position = screenPos;

            _scaleSequence?.Kill();
            _fadeSequence?.Kill();

            _scaleSequence = DOTween.Sequence();
            _fadeSequence = DOTween.Sequence();

            _scaleSequence
                .Append(Transform
                .DOScale(1, ScaleDuration)
                .SetEase(Ease.OutCirc));

            _fadeSequence
                .Append(_canvasGroup
                .DOFade(1, ScaleDuration)
                .SetEase(Ease.OutElastic));

            _scaleSequence.Play();
            _fadeSequence.Play();

            _canvasGroup.interactable = true;
            _closeButton.gameObject.SetActive(true);
        }

        private void HandleDisappear(InputAction.CallbackContext callbackContext)
        {
            // leftButton is disabled in InteractionController HandleView
            _inputSystem.ToggleBinding("<Mouse>/leftButton", true);
            Disappear();
        }

        /// <summary>
        /// Tweens the UI on the disabled position
        /// </summary>
        private void Disappear()
        {
            _scaleSequence?.Kill();
            _fadeSequence?.Kill();

            _scaleSequence = DOTween.Sequence();
            _fadeSequence = DOTween.Sequence();

            _scaleSequence
                .Append(Transform
                .DOScale(0, ScaleDuration)
                .SetEase(Ease.OutCirc));

            _fadeSequence.Append(_canvasGroup
                .DOFade(0, ScaleDuration)
                .SetEase(Ease.OutElastic));

            _scaleSequence.Play();
            _fadeSequence.Play();

            _canvasGroup.interactable = false;

            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(false);
            }

            ResetInteractionsMenu();
        }

        /// <summary>
        /// Clears the interactions menu
        /// </summary>
        private void ResetInteractionsMenu()
        {
            foreach (RadialInteractionButton interactionButton in _petalPool)
            {
                interactionButton.OnInteractionSelected -= HandleInteractionButtonPressed;
                interactionButton.Reset();
            }

            Interactions?.Clear();
            Event = null;
        }

        /// <summary>
        /// Updates the interactions that are available on the menu
        /// </summary>
        public void SetInteractions(List<IInteraction> interactions, InteractionEvent interactionEvent, Vector3 mousePosition)
        {
            Interactions = interactions;
            Event = interactionEvent;
        }
    }
}
