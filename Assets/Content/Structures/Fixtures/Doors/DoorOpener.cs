using System;
using System.Collections;
using Mirror;
using UnityEngine;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;

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
    public class DoorOpener : MonoBehaviour, Interaction
    {
        private const float DOOR_WAIT_CLOSE_TIME = 2.0f;

        [SerializeField] private AudioClip openSound = null;
        [SerializeField] private AudioClip closeSound = null;
        [SerializeField] private LayerMask doorTriggerLayers = -1;

        private bool openState; // Server and Client
        private int playersInTrigger; // Server Only
        private Coroutine closeTimer; // Server Only

        private Animator animator;
        private AudioSource audioSource;

        // Interaction stuff
        
        public InteractionEvent Event { get; set; }
        public string Name => openState ? "Close Door" : "Open Door";
        public bool CanInteract() => true;
        public void Interact() {
            CommunicateDoorState(!openState);

            if (openState == true) { // If we are now open, we need to start the timer to close again
                // Wait an additional amount of time for the door to open before closing again
                closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
            }
        }

        public void OnSetDoorState(bool open)
        {
            if (openState == open)
                return;
            openState = open;

            animator.SetBool("Open", open);
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

        public void Start()
        {
            // base.OnStartClient();
        
            audioSource = GetComponent<AudioSource>();
            animator = GetComponent<Animator>();

            // TODO: Sometime when we're not using a shitty networker
            // if (isClientOnly)
            //     OnSetDoorState(openState);
        }
    
        public void OnTriggerEnter(Collider other)
        {
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0 || !TileManager.IsOnServer(gameObject)) return;

            if (playersInTrigger == 0)
            {
                if (closeTimer != null) {
                    StopCoroutine(closeTimer);
                    closeTimer = null;
                }

                CommunicateDoorState(true);
            }

            playersInTrigger += 1;
        }

        public void OnTriggerExit(Collider other)
        {
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0 || !TileManager.IsOnServer(gameObject)) return;

            if (playersInTrigger == 1)
            {
                // Start the close timer (which may be stopped).
                closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
            }

            playersInTrigger = Math.Max(playersInTrigger - 1, 0);
        }

        private void CommunicateDoorState(bool open)
        {
            OnSetDoorState(open);
            transform.root.gameObject.GetComponent<TileServerManager>().SetDoorOpen(transform.parent.gameObject, open);
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            CommunicateDoorState(false);
        }
    }
}
