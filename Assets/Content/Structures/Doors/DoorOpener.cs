using System;
using System.Collections;
using Mirror;
using SS3D.Content.Furniture;
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
    public class DoorOpener : Openable
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

        protected override void OnOpenStateChange(object sender, bool open)
        {
            base.OnOpenStateChange(sender, open);
            CommunicateDoorState(open);
            if (open)
            {
                closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
            }
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

        public override void Start()
        {
            base.Start();
            audioSource = GetComponent<AudioSource>();
            animator = GetComponent<Animator>();

            // TODO: Sometime when we're not using a shitty networker
            // if (isClientOnly)
            //     OnSetDoorState(openState);

            TileObject tile = this.GetComponentInParent<TileObject>();
            tile.atmos?.SetBlocked(true);
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

                SetOpen(true);
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
            transform.root.gameObject.GetComponent<TileServerManager>().SetDoorOpen(transform.parent.gameObject, open);

            TileObject tile = this.GetComponentInParent<TileObject>();
            tile.atmos.SetBlocked(!open);
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            SetOpen(false);
            CommunicateDoorState(false);
        }
    }
}
