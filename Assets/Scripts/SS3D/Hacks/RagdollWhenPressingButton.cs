using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Hacks
{
    /// <summary>
    /// Currenytly using button Y to ragdoll, might change in the future.
    /// </summary>
    public class RagdollWhenPressingButton : NetworkActor
    {
        [SerializeField]
        private Ragdoll _ragdoll;

        [SerializeField]
        private float _timeRagdolled;

        private Controls.OtherActions _controls;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner) return;

            _controls = SubSystems.Get<InputSubSystem>().Inputs.Other;
            _controls.Ragdoll.performed += HandleKnockdown;
        }


        private void HandleKnockdown(InputAction.CallbackContext context)
        {
            _ragdoll.Knockdown(_timeRagdolled);
        }
    }
}

