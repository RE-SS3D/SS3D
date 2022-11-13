using SS3D.Systems.GameModes.GameEvents;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Items;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// The syndies love to use this to blow up the place
    /// </summary>
    public class NukeCard : Item
    {
        public GameEvent gameEvent;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new PickupNukeInteraction { pickupEvent = gameEvent } };
        }
    }
}