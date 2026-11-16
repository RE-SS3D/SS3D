using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Interactions
{
    public struct RadialInteractionItem
    {
        public Sprite Icon;
        public string InteractionName;
        public string SourceObjectName;
        public IInteraction Interaction;
        public InteractionTier Tier;

        public RadialInteractionItem(Sprite icon, string interactionName, IInteraction interaction, string sourceObjectName, InteractionTier tier = InteractionTier.Instant)
        {
            Icon = icon;
            InteractionName = interactionName;
            Interaction = interaction;
            SourceObjectName = sourceObjectName;
            Tier = tier;
        }
    }
}
