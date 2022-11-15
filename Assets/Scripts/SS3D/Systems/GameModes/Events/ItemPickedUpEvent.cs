using Coimbra.Services.Events;
using UnityEngine;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct ItemPickedUpEvent : IEvent
    {
        public string ItemName;
        public string OwnerName;

        public ItemPickedUpEvent(string itemName, string ownerName)
        {
            ItemName = itemName;
            OwnerName = ownerName;
        }
    }
}