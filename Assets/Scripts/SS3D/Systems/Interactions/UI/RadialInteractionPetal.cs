using System;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions.UI
{
    [UxmlElement]
    public partial class RadialInteractionPetal : VisualElement
    {
        private readonly VisualElement _icon;
        private readonly VisualElement _reticleBadge;
        private readonly VisualElement _reticleDot;

        public RadialInteractionPetal()
        {
            AddToClassList("radial-interaction-petal");
            pickingMode = PickingMode.Position;

            _icon = new VisualElement();
            _icon.AddToClassList("radial-interaction-petal__icon");
            _icon.pickingMode = PickingMode.Ignore;

            _reticleBadge = new VisualElement();
            _reticleBadge.AddToClassList("radial-interaction-petal__reticle-badge");
            _reticleBadge.pickingMode = PickingMode.Ignore;

            _reticleDot = new VisualElement();
            _reticleDot.AddToClassList("radial-interaction-petal__reticle-dot");
            _reticleBadge.Add(_reticleDot);

            Add(_icon);
            Add(_reticleBadge);

            RegisterCallback<ClickEvent>(HandleClick);
        }

        public IInteraction Interaction { get; private set; }

        public event Action<IInteraction> Clicked;

        public void Bind(Sprite icon, InteractionTier tier)
        {
            _icon.style.backgroundImage = icon != null ? new StyleBackground(icon) : StyleKeyword.None;
            bool showBadge = tier is InteractionTier.Targeted or InteractionTier.Combine;
            _reticleBadge.style.display = showBadge ? DisplayStyle.Flex : DisplayStyle.None;

            if (showBadge)
            {
                bool isCombine = tier == InteractionTier.Combine;
                _reticleBadge.EnableInClassList("radial-interaction-petal__reticle-badge--combine", isCombine);
                _reticleDot.EnableInClassList("radial-interaction-petal__reticle-dot--combine", isCombine);
            }
        }

        public void SetInteraction(IInteraction interaction, Sprite icon, InteractionTier tier)
        {
            Interaction = interaction;
            Bind(icon, tier);
            style.display = DisplayStyle.Flex;
        }

        public void ResetPetal()
        {
            Interaction = null;
            _icon.style.backgroundImage = StyleKeyword.None;
            _reticleBadge.style.display = DisplayStyle.None;
            style.display = DisplayStyle.None;
        }

        private void HandleClick(ClickEvent _)
        {
            if (Interaction != null)
            {
                Clicked?.Invoke(Interaction);
            }
        }
    }
}
