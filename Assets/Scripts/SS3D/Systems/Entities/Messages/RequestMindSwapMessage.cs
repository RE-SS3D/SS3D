using FishNet.Broadcast;
using UnityEngine;

namespace SS3D.Systems.Entities.Messages
{
    public struct RequestMindSwapMessage : IBroadcast
    {
        public readonly GameObject Origin;
        public readonly GameObject Target;

        public RequestMindSwapMessage(GameObject origin, GameObject target)
        {
            Origin = origin;
            Target = target;
        }
    }
}