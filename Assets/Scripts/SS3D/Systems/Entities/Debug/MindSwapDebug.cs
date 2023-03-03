using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Entities.Debug
{
    public class MindSwapDebug : NetworkActor
    {
        public Entity Origin;
        public Entity Target;
        private Controls.OtherActions _controls;

        protected override void OnEnabled()
        {
            base.OnEnabled();

			_controls = SystemLocator.Get<InputSystem>().Inputs.Other;
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

            MindSystem mindSystem = SystemLocator.Get<MindSystem>();
            mindSystem.CmdSwapMinds(Origin, Target);

            Origin = Target;
            Target = Origin;
        }
    }
}
