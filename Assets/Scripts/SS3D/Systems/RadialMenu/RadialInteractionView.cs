using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.RadialMenu
{
    public class RadialInteractionView : SpessBehaviour
    {
        public event Action<IInteraction> OnInteractionSelected;

        [Header("UI")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _indicator;

        [SerializeField] private Sprite _missingIcon;

        [Header("Buttons")]
        [SerializeField] private List<RadialInteractionButton> _interactionButtons;

        private GameObject _selectedObject;
        private  IInteraction _selectedInteraction;

        private Sequence _scaleSequence;
        private Sequence _fadeSequence;

        private const float ScaleDuration = .07f;

        private List<IInteraction> Interactions { get; set; }

        private InteractionEvent Event { get; set; }

        protected override void OnStart()
        {
            base.OnStart();

            Disappear();
            Setup();
        }

        private void Setup()
        {
            foreach (RadialInteractionButton interactionButton in _interactionButtons)
            {
                interactionButton.OnHovered += HandleInteractionButtonHovered;
            }
        }

        private void HandleInteractionButtonHovered(GameObject button, IInteraction interaction)
        {
            _selectedObject = button;
            _selectedInteraction = interaction;
        }

        private void HandleInteractionButtonPressed(IInteraction interaction)
        {
            Disappear();
            OnInteractionSelected?.Invoke(interaction);
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

        private void UpdateIndicator()
        {
            Vector3 mouse = Input.mousePosition;
            Vector3 dir = (mouse - _indicator.position).normalized;

            if (_selectedObject != null)
            {
                _indicator.eulerAngles = new Vector3(0, 0, _selectedObject.transform.eulerAngles.z);
            }
        }

        public void SetInteractions(List<IInteraction> interactions, InteractionEvent interactionEvent, Vector3 mousePosition)
        {
            Interactions = interactions;
            Event = interactionEvent;
        }

        public void ShowInteractionsMenu()
        {
            if (Event == null || Interactions == null || Interactions.Count < 1)
            {
                return;
            }

            ResetInteractionsMenu();
            
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

        private void Show()
        {
            Vector2 screenPos = new(Input.mousePosition.x, Input.mousePosition.y);

            Position = screenPos;

            _scaleSequence?.Kill();
            _fadeSequence?.Kill();

            _scaleSequence = DOTween.Sequence();
            _fadeSequence = DOTween.Sequence();

            _scaleSequence.Append(TransformCache.DOScale(1, ScaleDuration).SetEase(Ease.InCirc));
            _fadeSequence.Append(_canvasGroup.DOFade(1, ScaleDuration).SetEase(Ease.InElastic));

            _scaleSequence.Play();
            _fadeSequence.Play();

            _canvasGroup.interactable = true;
        }

        private void Disappear()
        {
            _scaleSequence?.Kill();
            _fadeSequence?.Kill();

            _scaleSequence = DOTween.Sequence();
            _fadeSequence = DOTween.Sequence();

            _scaleSequence.Append(TransformCache.DOScale(0, ScaleDuration).SetEase(Ease.InCirc));
            _fadeSequence.Append(_canvasGroup.DOFade(0, ScaleDuration).SetEase(Ease.InElastic));

            _scaleSequence.Play();
            _fadeSequence.Play();

            _canvasGroup.interactable = false;
        }

        private void ResetInteractionsMenu()
        {
            foreach (RadialInteractionButton interactionButton in _interactionButtons)
            {
                interactionButton.Reset();
            }

            //_interactions.Clear();
            _selectedInteraction = null;
            _selectedObject = null;
            //_interactionEvent = null;
        }

        private RadialInteractionButton GetAvailableButton()
        {
            return _interactionButtons.First(button => !button.Occupied);
        }
    }
}