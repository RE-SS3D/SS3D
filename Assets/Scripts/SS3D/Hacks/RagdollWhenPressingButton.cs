using SS3D.Core;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Hacks
{
    /// <summary>
    /// Currenytly using button Y to ragdoll, might change in the future.
    /// </summary>
    public class RagdollWhenPressingButton : MonoBehaviour
    {
        [SerializeField]
        private Ragdoll _ragdoll;

        [SerializeField]
        private float _timeRagdolled;

        private Controls.OtherActions _controls;

        private void Start()
        {
            _controls = Subsystems.Get<InputSystem>().Inputs.Other;
            _controls.Ragdoll.performed += HandleKnockdown;
        }

        private void HandleKnockdown(InputAction.CallbackContext context)
        {
            _ragdoll.Knockdown(_timeRagdolled);
        }
    }
}

