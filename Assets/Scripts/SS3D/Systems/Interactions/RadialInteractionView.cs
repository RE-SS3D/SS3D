﻿using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SS3D.Core.Behaviours;
using SS3D.Core.Utils;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Controls the UI for a radial interaction menu
    /// </summary>
    public sealed class RadialInteractionView : Core.Behaviours.System
    {
        public event Action<IInteraction> OnInteractionSelected;

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

        protected override void OnStart()
        {
            base.OnStart();

            Setup();
            Disappear();
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            if (Input.GetMouseButtonUp(1))
            {
                Disappear();
            }

            UpdateIndicator();
        }   

        private void Setup()                            
        {
            Interactions = new List<IInteraction>();

            foreach (RadialInteractionButton interactionButton in _interactionButtons)
            {
                interactionButton.OnHovered += HandleInteractionButtonHovered;
            }
        }

        private void HandleInteractionButtonHovered(GameObject button, IInteraction interaction)
        {
            _selectedObject = button;
        }

        private void HandleInteractionButtonPressed(IInteraction interaction)
        {
            Disappear();
            OnInteractionSelected?.Invoke(interaction);
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
            Vector2 screenPos = new(Input.mousePosition.x, Input.mousePosition.y);
            Position = screenPos;

            _selectedObject = _interactionButtons.First().GameObjectCache;
            Interactions.First();
            
            UpdateIndicator();

            _scaleSequence?.Kill();
            _fadeSequence?.Kill();

            _scaleSequence = DOTween.Sequence();
            _fadeSequence = DOTween.Sequence();

            _scaleSequence
                .Append(TransformCache
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
                .Append(TransformCache
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