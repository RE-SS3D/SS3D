using Coimbra.Services.Events;
using UnityEngine;

namespace SS3D.Systems.Entities.Events
{
    public partial struct LocalPlayerObjectChanged : IEvent
    {
        public readonly GameObject PlayerObject;

        public readonly bool PlayerHasObject;

        public LocalPlayerObjectChanged(GameObject playerObject, bool playerHasObject)
        {
            PlayerObject = playerObject;
            PlayerHasObject = playerHasObject;
        }
    }
}