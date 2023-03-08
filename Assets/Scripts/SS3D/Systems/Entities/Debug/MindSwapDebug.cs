using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Entities.Debug
{
    public sealed class MindSwapDebug : NetworkActor
    {
        public Entity Origin;
        public Entity Target;
        private Controls.OtherActions _controls;

        protected override void OnStart()
        {
            base.OnStart();

            _controls = Subsystems.Get<InputSubsystem>().Inputs.Other;
            _controls.SwapMinds.performed += HandleMindSwap;
        }
        
        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            _controls.SwapMinds.performed -= HandleMindSwap;
        }

        [ContextMenu("Request Mind Swap")]
        public void HandleMindSwap(InputAction.CallbackContext callbackContext)
        {
            if (Origin == null || Target == null)
            {
                return;
            }

            MindSubsystem mindSubsystem = Subsystems.Get<MindSubsystem>();
            mindSubsystem.CmdSwapMinds(Origin, Target);

            Origin = Target;
            Target = Origin;
        }
    }
}
