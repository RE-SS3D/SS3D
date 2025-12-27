using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private RectTransform _indicator;

        [SerializeField] private Sprite _missingIcon;

        [Header("Buttons")]
        [SerializeField] private List<RadialInteractionButton> _interactionButtons;

        private GameObject _selectedObject;

        private Sequence _rotateSequence;
        private Sequence _scaleSequence;
        private Sequence _fadeSequence;
        private Sequence _indicatorRotateSequence;

        private const float ScaleDuration = .2f;
        private const float PetalRotateDuration = .02f;

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

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            UpdateIndicator();
        }

        private void Setup()
        {
            AddHandle(UpdateEvent.AddListener(HandleUpdate));

            Interactions = new List<IInteraction>();

            foreach (RadialInteractionButton interactionButton in _interactionButtons)
            {
                interactionButton.OnHovered += HandleInteractionButtonHovered;
            }

            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs.Interactions;
        }

        private void HandleInteractionButtonHovered(GameObject button, IInteraction interaction)
        {
            _selectedObject = button;
        }

        private void HandleInteractionButtonPressed(IInteraction interaction, RadialInteractionButton radialInteractionButton)
        {
            radialInteractionButton.OnInteractionSelected -= HandleInteractionButtonPressed;

            Disappear();
            OnInteractionSelected?.Invoke(interaction, radialInteractionButton);
        }

        /// <summary>
        /// Updates the indicator to the current selected interaction
        /// </summary>
        private void UpdateIndicator()
        {
            if (_selectedObject == null)
            {
                return;
            }

            _indicatorRotateSequence?.Kill();
            _indicatorRotateSequence = DOTween.Sequence();

            //_indicator.eulerAngles = new Vector3(0, 0, z);
            float z = _selectedObject.transform.eulerAngles.z;
            Vector3 rotation = new(0, 0, z);

            // Rotates the petal to the selected interaction
            _indicatorRotateSequence.Append(_indicator
                .DORotate(rotation, PetalRotateDuration)
                .SetEase(Ease.InCirc));

            _indicatorRotateSequence.Play();
        }

        /// <summary>
        /// Opens the interaction menu
        /// </summary>
        public void ShowInteractionsMenu()
        {
            bool hasInteractions = Event != null && !Interactions.IsNullOrEmpty();
            if (!hasInteractions) { return; }

            InteractionFolder folder = new();
            foreach (IInteraction interaction in Interactions)
            {
                Sprite icon = interaction.GetIcon(Event);

                if (icon == null)
                {
                    icon = _missingIcon;
                }

                string interactionName = interaction.GetName(Event);
                string objectName = Event.Target.ToString();

                RadialInteractionItem radialInteractionItem = new(icon, interactionName, interaction, objectName);
                folder.AddInteraction(radialInteractionItem);

                RadialInteractionButton interactionButton = GetAvailableButton();

                interactionButton.SetInteraction(radialInteractionItem);
                interactionButton.OnInteractionSelected += HandleInteractionButtonPressed;
            }

            Show();
        }

        /// <summary>
        /// Tweens the UI on the enabled position
        /// </summary>
        private void Show()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Position = screenPos;

            _selectedObject = _interactionButtons.First().GameObject;

            UpdateIndicator();

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

            ResetInteractionsMenu();
        }

        /// <summary>
        /// Clears the interactions menu
        /// </summary>
        private void ResetInteractionsMenu()
        {
            foreach (RadialInteractionButton interactionButton in _interactionButtons)
            {
                interactionButton.Reset();
            }

            Interactions.Clear();
            _selectedObject = null;
            Event = null;
        }

        /// <summary>
        /// Updates the interactions that are available on the menu
        /// </summary>
        /// <param name="interactions">Interaction list</param>
        /// <param name="interactionEvent">Interaction event</param>
        /// <param name="mousePosition">Mouse position when the interaction was created</param>
        public void SetInteractions(List<IInteraction> interactions, InteractionEvent interactionEvent, Vector3 mousePosition)
        {
            Interactions = interactions;
            Event = interactionEvent;
        }

        /// <summary>
        /// Gets a button that is not used
        /// </summary>
        /// <returns></returns>
        private RadialInteractionButton GetAvailableButton()
        {
            return _interactionButtons.First(button => !button.Occupied);
        }
    }
}
