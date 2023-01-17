using SS3D.Core;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Entities.Debug
{
    public class MindSwapDebug : NetworkActor
    {
        public Entity Origin;
        public Entity Target;

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

            MindSystem mindSystem = SystemLocator.Get<MindSystem>();
            mindSystem.CmdSwapMinds(Origin, Target);

            Origin = Target;
            Target = Origin;
        }
    }
}
