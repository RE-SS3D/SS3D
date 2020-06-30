using Mirror;
using SS3D.Content.Systems.Player;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Content.Creatures.Human
{
    [RequireComponent(typeof(Animator))]
    public class HumanRagdoll : NetworkBehaviour
    {
        public Transform ArmatureRoot;
        
        /// <summary>
        /// Is the ragdoll enabled
        /// </summary>
        public bool BodyEnabled
        {
            get => bodyEnabled;
            set => SetEnabled(value);
        }

        private bool bodyEnabled = false;
        private Transform character;
        private Transform center;
    
        void Start()
        {
            Assert.IsNotNull(ArmatureRoot);
            SetEnabled(BodyEnabled);
            character = ArmatureRoot.parent;
            center = ArmatureRoot.GetChild(0);
        }

        void Update()
        {
            if (bodyEnabled && character != null && center != null)
            {
                character.position = center.position;
                center.localPosition = Vector3.zero;
            }
        }

        public void SetEnabled(bool enabled)
        {
            bodyEnabled = enabled;

            HumanoidMovementController movementController = GetComponent<HumanoidMovementController>();
            Vector3 movement = Vector3.zero;
            if (movementController != null)
            {
                movement = movementController.absoluteMovement;
            }
            
            foreach (Rigidbody body in ArmatureRoot.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = !enabled;
                if (enabled)
                {
                    body.AddForce(movement, ForceMode.VelocityChange);
                }
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
