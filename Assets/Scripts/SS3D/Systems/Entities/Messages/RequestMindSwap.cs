using FishNet.Broadcast;
using UnityEngine;

namespace SS3D.Systems.Entities.Messages
{
    public struct RequestMindSwap : IBroadcast
    {
        public readonly GameObject Origin;
        public readonly GameObject Target;

        public RequestMindSwap(GameObject origin, GameObject target)
        {
            Origin = origin;
            Target = target;
        }
    }
}