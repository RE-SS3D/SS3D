using Coimbra.Services.Events;
using SS3D.Systems.Storage.Items;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct ItemPickedUpEvent : IEvent
    {
        public readonly Item Item;
        public readonly string Player;

        public ItemPickedUpEvent(Item item, string player)
        {
            Item = item;
            Player = player;
        }
    }
}