using Coimbra.Services.Events;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Camera.Events
{
    public partial struct CameraTargetChanged : IEvent
    {
        public readonly GameObject Target;

        public CameraTargetChanged(GameObject target)
        {
            Target = target;
        }
    }
}
