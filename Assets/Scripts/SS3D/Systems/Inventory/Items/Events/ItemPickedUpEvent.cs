using Coimbra.Services.Events;

namespace SS3D.Systems.Inventory.Items
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
