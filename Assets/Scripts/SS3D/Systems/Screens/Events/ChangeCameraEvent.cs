using Coimbra.Services.Events;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Screens.Events
{
    public partial struct ChangeCameraEvent : IEvent
    {
        public readonly GameObject Target;

        public ChangeCameraEvent(GameObject target)
        {
            Target = target;
        }
    }
}