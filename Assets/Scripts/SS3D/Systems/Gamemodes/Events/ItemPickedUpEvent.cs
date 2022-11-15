using Coimbra.Services.Events;
using UnityEngine;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct ItemPickedUpEvent : IEvent
    {
        public readonly string ItemName;
        public readonly string OwnerName;

        public ItemPickedUpEvent(string itemName, string ownerName)
        {
            ItemName = itemName;
            OwnerName = ownerName;
        }
    }
}