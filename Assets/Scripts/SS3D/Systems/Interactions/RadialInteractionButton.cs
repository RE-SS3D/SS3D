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
        public bool Occupied;

        [SerializeField] private TMP_Text _interactionNameText;
        [SerializeField] private Image _interactionIcon;
        [SerializeField] private Button _button;

        public Button.ButtonClickedEvent Pressed => _button.onClick;
        public IInteraction Interaction { get; private set; }

        public event Action<IInteraction> OnInteractionSelected;
        public event Action<GameObject, IInteraction> OnHovered;

        private const float MinimumThreshold = 0.5f;

        protected override void OnStart()
        {
            base.OnStart();

            _button.image.alphaHitTestMinimumThreshold = MinimumThreshold;
        }

        public void SetInteraction(RadialInteractionItem interactionItem)
        {
            if (Occupied)
            {
                Reset();
            }

            GameObjectCache.SetActive(true);
            _interactionIcon.enabled = true;
            _interactionIcon.sprite = interactionItem.Icon;
            _interactionNameText.SetText(interactionItem.InteractionName);
            Interaction = interactionItem.Interaction;

            Pressed.AddListener(HandleButtonPressed);

            Occupied = true;
        }

        private void HandleButtonPressed()
        {
            OnInteractionSelected?.Invoke(Interaction);
        }

        public void Reset()
        {
            GameObjectCache.SetActive(false);
            _interactionIcon.enabled = false;
            _interactionIcon.sprite = null;
            _interactionNameText.SetText(string.Empty);
            Interaction = null;

            Pressed.RemoveListener(HandleButtonPressed);

            Occupied = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHovered?.Invoke(GameObjectCache, Interaction);
        }
    }
}
