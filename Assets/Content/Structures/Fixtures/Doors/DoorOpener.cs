using System;
using System.Collections;
using Mirror;
using UnityEngine;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Structures.Fixtures
{
    /**
     * Animates the door opening and closing.
     * 
     * Behaviour:
     *  - Door opens when user is within the attached 'trigger' collider
     *  - Door closes DOOR_WAIT_CLOSE_TIME after the last person leaves it's collider.
     *    - If a user re-enters and leaves the collider, the timer for closing resets
     * 
     * Note: Eventually the 'door open' state should
     *      probably be stored in the same place as wires and other state?
     */
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Animator))]
    public class DoorOpener : NetworkBehaviour, Interaction
    {
        private const float DOOR_WAIT_CLOSE_TIME = 2.0f;

        [SerializeField] private AudioClip openSound = null;
        [SerializeField] private AudioClip closeSound = null;
        [SerializeField] private LayerMask doorTriggerLayers = -1;

        [SyncVar(hook = "OnDoorStateChange")]
        private bool openState; // Server Only

        private int playersInTrigger; // Server Only
        private Coroutine closeTimer; // Server Only
        // Used for ensuring correct timing on certain actions
        private DateTime lastInteraction; // Server Only

        private Animator animator;
        private AudioSource audioSource;

        // Interaction stuff
        
        public InteractionEvent Event { get; set; }
        public string Name => openState ? "Close Door" : "Open Door";
        public bool CanInteract() => true;
        public void Interact() {
            openState = !openState;

            if (openState == true) { // If we are now open, we need to start the timer to close again
                // Wait an additional amount of time for the door to open before closing again
                closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
            }

            lastInteraction = DateTime.Now;
        }

        /// <summary>
        /// Gets called by AnimationEvents
        /// </summary>
        /// <param name="clipType">"open" or "close"</param>
        public void PlaySound(string clipType)
        {
            audioSource.PlayOneShot(clipType == "open" ? openSound : closeSound);
        }

        // Overriding (non-interesting) methods

        public void OnValidate()
        {
            if (doorTriggerLayers == -1)
                doorTriggerLayers = LayerMask.NameToLayer("Player");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        
            audioSource = GetComponent<AudioSource>();
            animator = GetComponent<Animator>();

            if (isClientOnly)
                OnDoorStateChange(openState);
        }
    
        public void OnTriggerEnter(Collider other)
        {
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0 || !isServer) return;

            if (playersInTrigger == 0)
            {
                if (closeTimer != null) {
                    StopCoroutine(closeTimer);
                    closeTimer = null;
                }

                openState = true;
            }

            playersInTrigger += 1;
        }

        public void OnTriggerExit(Collider other)
        {
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0 || !isServer) return;

            if (playersInTrigger == 1)
            {
                // Start the close timer (which may be stopped).
                closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
            }

            playersInTrigger = Math.Max(playersInTrigger - 1, 0);
        }

        private void OnDoorStateChange(bool open)
        {
            if (openState == open)
                return;
            openState = open;

            if (isClient)
            {
                animator.SetBool("Open", open);
            }
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            openState = false;
        }
    }
}
