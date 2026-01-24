using FishNet.Connection;
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

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            if (IsOwner)
            {
                SubscribeToInput();
            }
            else if (prevOwner.Equals(LocalConnection))
            {
                UnsubscribeFromInput();
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            InputSubSystem inputSubSystem = SubSystems.Get<InputSubSystem>();

            if (inputSubSystem)
            {
                _controls = inputSubSystem.Inputs.Other;
            }
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            if (IsOwner)
            {
                SubscribeToInput();
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            if (IsOwner)
            {
                UnsubscribeFromInput();
            }
        }

        private void SubscribeToInput()
        {
            _controls.Ragdoll.performed += HandleKnockdown;
        }

        /// <summary>
        /// Unsubscribes from input events
        /// </summary>
        private void UnsubscribeFromInput()
        {
            _controls.Ragdoll.performed -= HandleKnockdown;
        }

        private void HandleKnockdown(InputAction.CallbackContext context)
        {
            _ragdoll.Knockdown(_timeRagdolled);
        }
    }
}