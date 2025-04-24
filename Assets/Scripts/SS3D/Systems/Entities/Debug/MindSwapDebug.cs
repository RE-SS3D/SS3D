using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Entities.Debug
{
    public class MindSwapDebug : NetworkActor
    {
        public Entity Origin;
        public Entity Target;
        private Controls.OtherActions _controls;

        protected override void OnStart()
        {
            base.OnStart();
            
            // _controls = Subsystems.Get<InputSystem>().Inputs.Other;
            // _controls.SwapMinds.performed += HandleMindSwap;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
           // _controls.SwapMinds.performed -= HandleMindSwap;
        }

        [ContextMenu("Request Mind Swap")]
        public void HandleMindSwap(InputAction.CallbackContext callbackContext)
        {
            if (Origin == null || Target == null)
            {
                return;
            }

            MindSubSystem mindSystem = SubSystems.Get<MindSubSystem>();
            mindSystem.CmdSwapMinds(Origin, Target);

            Origin = Target;
            Target = Origin;
        }
    }
}
