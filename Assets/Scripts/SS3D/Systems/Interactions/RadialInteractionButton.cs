using System;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Systems.Interactions
{
    public class RadialInteractionButton : Actor, IPointerEnterHandler
    {
        public bool Occupied;

        [SerializeField] private Image _interactionIcon;
        [SerializeField] private Image _reticleBadge;
        [SerializeField] private Button _button;

        private IInteraction _interaction;

        public IInteraction Interaction => _interaction;

        public Button.ButtonClickedEvent Pressed => _button.onClick;
        public event Action<IInteraction, RadialInteractionButton> OnInteractionSelected;
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

            GameObject.SetActive(true);
            _interactionIcon.enabled = true;
            _interactionIcon.sprite = interactionItem.Icon;
            _interaction = interactionItem.Interaction;

            SetReticleBadge(interactionItem.Tier is InteractionTier.Targeted or InteractionTier.Combine, interactionItem.Tier);

            Pressed.AddListener(HandleButtonPressed);

            Occupied = true;
        }

        public void SetReticleBadge(bool visible, InteractionTier tier)
        {
            if (_reticleBadge == null)
            {
                return;
            }

            _reticleBadge.gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            _reticleBadge.color = tier switch
            {
                InteractionTier.Combine => new Color(0.533f, 0.702f, 0.416f),
                _ => new Color(0.773f, 0.310f, 0.188f),
            };
        }

        private void HandleButtonPressed()
        {
            OnInteractionSelected?.Invoke(_interaction, this);
        }

        public void Reset()
        {
            GameObject.SetActive(false);
            _interactionIcon.enabled = false;
            _interactionIcon.sprite = null;
            _interaction = null;

            SetReticleBadge(false, InteractionTier.Instant);

            Pressed.RemoveListener(HandleButtonPressed);

            Occupied = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHovered?.Invoke(GameObject, _interaction);
        }
    }
}
