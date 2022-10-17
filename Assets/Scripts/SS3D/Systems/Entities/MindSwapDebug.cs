using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Messages;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    public class MindSwapDebug : NetworkedSpessBehaviour
    {
        public GameObject Origin;
        public GameObject Target;

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            if (Input.GetKeyDown(KeyCode.M))
            {
                DoMindSwap();
            }
        }

        [ContextMenu("Request Mind Swap")]
        public void DoMindSwap()
        {
            if (Origin == null || Target == null)
            {
                return;
            }

            RequestMindSwap mindSwap = new(Origin, Target);
            ClientManager.Broadcast(mindSwap);

            Origin = mindSwap.Target;
            Target = mindSwap.Origin;
        }
    }
}
