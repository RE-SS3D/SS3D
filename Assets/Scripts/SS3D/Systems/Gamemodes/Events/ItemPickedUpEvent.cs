using Coimbra.Services.Events;
using SS3D.Systems.Storage.Items;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct ItemPickedUpEvent : IEvent
    {
        public readonly Item ItemRef;
        public readonly string Player;

        public ItemPickedUpEvent(Item itemRef, string player)
        {
            ItemRef = itemRef;
            Player = player;
        }
    }
}