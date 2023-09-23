using System;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Systems.Interactions
{
    public class RadialInteractionButton : Actor, IPointerEnterHandler
    {
        public event Action<IInteraction, RadialInteractionButton> OnInteractionSelected;

        public event Action<GameObject, IInteraction> OnHovered;

        public bool Occupied;

        private const float MinimumThreshold = 0.5f;

        [SerializeField]
        private TMP_Text _interactionNameText;

        [SerializeField]
        private Image _interactionIcon;

        [SerializeField]
        private Button _button;

        private IInteraction _interaction;

        public IInteraction Interaction
        {
            get { return _interaction; }
        }

        public Button.ButtonClickedEvent Pressed => _button.onClick;

        public void SetInteraction(RadialInteractionItem interactionItem)
        {
            if (Occupied)
            {
                Reset();
            }

            GameObject.SetActive(true);
            _interactionIcon.enabled = true;
            _interactionIcon.sprite = interactionItem.Icon;
            _interactionNameText.SetText(interactionItem.InteractionName);
            _interaction = interactionItem.Interaction;

            Pressed.AddListener(HandleButtonPressed);

            Occupied = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHovered?.Invoke(GameObject, _interaction);
        }

        #pragma warning disable UNT0021
        public void Reset()
            #pragma warning restore UNT0021
        {
            GameObject.SetActive(false);
            _interactionIcon.enabled = false;
            _interactionIcon.sprite = null;
            _interactionNameText.SetText(string.Empty);
            _interaction = null;

            Pressed.RemoveListener(HandleButtonPressed);

            Occupied = false;
        }

        protected override void OnStart()
        {
            base.OnStart();

            _button.image.alphaHitTestMinimumThreshold = MinimumThreshold;
        }

        private void HandleButtonPressed()
        {
            OnInteractionSelected?.Invoke(_interaction, this);
        }
    }
}
