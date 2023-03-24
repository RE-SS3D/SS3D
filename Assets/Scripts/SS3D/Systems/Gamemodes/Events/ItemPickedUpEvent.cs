using Coimbra.Services.Events;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct ItemPickedUpEvent : IEvent
    {
        public readonly ItemActor Item;
        public readonly string Player;

        public ItemPickedUpEvent(ItemActor item, string player)
        {
            Item = item;
            Player = player;
        }
    }
}