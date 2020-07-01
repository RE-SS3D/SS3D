using System;
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
            get;
            private set;
        }
        
        // The parent object of the armature
        private Transform character;
        // The center of the body
        private Transform center;
        // When the knockdown will expire
        private float knockdownEnd;
        // Will the ragdoll get up by itself?
        private bool isKnockedDown;
        private HumanoidMovementController movementController;
        private CharacterController characterController;

        void Start()
        {
            Assert.IsNotNull(ArmatureRoot);
            SetEnabledInternal(false);
            character = ArmatureRoot.parent;
            center = ArmatureRoot.GetChild(0);
        }

        void Update()
        {
            if (BodyEnabled)
            {
                if (character != null && center != null)
                {
                    // Make character follow center of body
                    character.position = center.position;
                    center.localPosition = Vector3.zero;
                }

                if (isKnockedDown && knockdownEnd < Time.time)
                {
                    // Knockdown expired
                    Recover();
                }
            }
        }

        /// <summary>
        /// Knocks this ragdoll down for the specific amount of time
        /// </summary>
        /// <param name="duration">How long the knockdown lasts</param>
        /// <param name="extend">If this extends any existing knockdown that is longer</param>
        public void KnockDown(float duration, bool extend = false)
        {
            if (duration < 0)
            {
                throw new ArgumentException("Can not knock down for a negative duration, use the Recover method", nameof(duration));
            }

            if (isKnockedDown)
            {
                float time = Time.time;
                float remainingTime = Mathf.Max(0, knockdownEnd - time);
                // Extend or fill up remaining time
                knockdownEnd = time + (extend ? remainingTime + duration : Mathf.Max(remainingTime, duration));
            }
            else
            {
                // Can't knockdown a body that has been manually disabled
                if (BodyEnabled)
                {
                    return;
                }
                
                isKnockedDown = true;
                knockdownEnd = Time.time + duration;
                SetEnabledInternal(true);
            }
        }

        /// <summary>
        /// Decreases knockdown by a specific amount
        /// </summary>
        /// <param name="time">How much faster the ragdoll recovers</param>
        public void Recover(float time)
        {
            if (!isKnockedDown)
            {
                return;
            }
            
            knockdownEnd -= time;
            if (knockdownEnd < Time.time)
            {
                Recover();
            }
        }
        
        /// <summary>
        /// Completely recovers from any knockdown
        /// </summary>
        public void Recover()
        {
            if (isKnockedDown)
            {
                isKnockedDown = false;
                SetEnabledInternal(false);
            }
        }

        /// <summary>
        /// Enables or disables the body until this method is called again
        /// </summary>
        /// <param name="enabled">If the body should be enabled or disabled</param>
        public void SetEnabled(bool enabled)
        {
            isKnockedDown = false;
            SetEnabledInternal(enabled);
        }
        
        private void SetEnabledInternal(bool enabled)
        {
            BodyEnabled = enabled;

            // Get absolute character movement
            movementController = movementController != null ? movementController : GetComponent<HumanoidMovementController>();
            Vector3 movement = Vector3.zero;
            if (movementController != null)
            {
                movement = movementController.absoluteMovement;
            }
            
            // For each rigid body in the ragdoll
            foreach (Rigidbody body in ArmatureRoot.GetComponentsInChildren<Rigidbody>())
            {
                // Set physics simulation
                body.isKinematic = !enabled;
                if (enabled)
                {
                    // Apply movement force to preserve momentum
                    body.AddForce(movement, ForceMode.VelocityChange);
                }
            }

            // Enable/disable animator
            GetComponent<Animator>().enabled = !enabled;
            
            // Enable/disable character controller
            characterController = characterController != null ? characterController : GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = !enabled;
            }
            
            // Enable/disable movement controller
            if (movementController != null)
            {
                movementController.enabled = !enabled;
            }

            // Replicate changes on client
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
