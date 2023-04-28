using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Entities.Debug
{
    public class CharacterSwapDebug : NetworkActor
    {
        public Entity Origin;
        public Entity Target;
        private Controls.OtherActions _controls;

        protected override void OnStart()
        {
            base.OnStart();
            
            _controls = Subsystems.Get<InputSystem>().Inputs.Other;
            _controls.SwapCharacters.performed += HandleCharacterSwap;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            _controls.SwapCharacters.performed -= HandleCharacterSwap;
        }

        [ContextMenu("Request Character Swap")]
        public void HandleCharacterSwap(InputAction.CallbackContext callbackContext)
        {
            if (Origin == null || Target == null)
            {
                return;
            }

            CharacterSystem characterSystem = Subsystems.Get<CharacterSystem>();
            characterSystem.CmdSwapCharacters(Origin, Target);

            Origin = Target;
            Target = Origin;
        }
    }
}
