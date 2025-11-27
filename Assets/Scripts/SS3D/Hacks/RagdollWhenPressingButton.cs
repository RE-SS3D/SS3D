using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Hacks
{
    /// <summary>
    /// Currenytly using button Y to ragdoll, might change in the future.
    /// </summary>
    public class RagdollWhenPressingButton : NetworkActor
    {
        [SerializeField]
        private PositionController _positionController;

        [SerializeField]
        private float _timeRagdolled;

        [SerializeField]
        private float _randomForce;

        private Controls.OtherActions _controls;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner) return;

            _controls = Subsystems.Get<InputSubSystem>().Inputs.Other;
            _controls.Ragdoll.performed += HandleKnockdown;
        }

        private void HandleKnockdown(InputAction.CallbackContext context)
        {
            if (_positionController.PositionType != PositionType.Ragdoll)
            {
                RpcKnockDown();
            }
            else
            {
                RpcRecover();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcKnockDown()
        {
            GetComponent<Ragdoll>().KnockDown();
            GetComponent<Ragdoll>().AddForceToAllParts(Random.insideUnitCircle * _randomForce);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcRecover()
        {
            GetComponent<Ragdoll>().Recover();
        }


    }
}
