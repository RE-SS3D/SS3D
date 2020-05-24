using Mirror;
using SS3D.Content.Systems.Player;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Content.Creatures.Human
{
    [RequireComponent(typeof(Animator))]
    public class HumanRigidBody : NetworkBehaviour
    {
        public Transform ArmatureRoot;
        public bool BodyEnabled
        {
            get => bodyEnabled;
            set => SetEnabled(value);
        }

        private bool bodyEnabled = false;
    
        void Start()
        {
            Assert.IsNotNull(ArmatureRoot);
            SetEnabled(BodyEnabled);
        }

        public void SetEnabled(bool enabled)
        {
            bodyEnabled = enabled;

            HumanoidMovementController movementController = GetComponent<HumanoidMovementController>();
            Vector3 movement = Vector3.zero;
            if (movementController != null)
            {
                movement = movementController.AbsoluteMovement;
            }
            
            foreach (Rigidbody body in ArmatureRoot.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = !enabled;
                body.AddForce(movement, ForceMode.VelocityChange);
            }

            GetComponent<Animator>().enabled = !enabled;
            
            CharacterController characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = !enabled;
            }
            
            if (movementController != null)
            {
                movementController.enabled = !enabled;
            }

            if (isServer)
            {
                RpcSetEnabled(enabled);
            }
        }

        [ClientRpc]
        private void RpcSetEnabled(bool enabled)
        {
            if (!isServer)
            {
                SetEnabled(enabled);
            }
        }
    }
}
