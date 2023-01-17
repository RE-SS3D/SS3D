using Coimbra.Services.Events;
using UnityEngine;

namespace SS3D.Systems.Entities.Events
{
    public partial struct LocalPlayerObjectChanged : IEvent
    {
        public readonly GameObject Target;

        public LocalPlayerObjectChanged(GameObject target)
        {
            Target = target;
        }
    }
}