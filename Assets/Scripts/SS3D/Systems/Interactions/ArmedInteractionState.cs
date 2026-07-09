using SS3D.Interactions;
using SS3D.Interactions.Interfaces;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Client-side state for a Tier 2 or Tier 3 interaction armed from the radial menu.
    /// </summary>
    public sealed class ArmedInteractionState
    {
        public IInteraction Interaction { get; init; }

        public InteractionEvent OriginEvent { get; init; }

        public InteractionTier Tier { get; init; }

        public string Label { get; init; }
    }
}
