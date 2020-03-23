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
    public class DoorOpener : NetworkBehaviour, Interaction
    {
        private const float DOOR_WAIT_CLOSE_TIME = 1.0f;
        private const float DOOR_TRANSITION_TIME = 0.6667f;

        [SerializeField] private Transform leftPanel = null;
        [SerializeField] private Transform rightPanel = null;

        [SerializeField] private AudioSource openSfx = null;
        [SerializeField] private AudioSource closeSfx = null;

        [SerializeField] private LayerMask doorTriggerLayers = -1;

        // Part of the animations
        [SerializeField] private Vector3 openLeft = new Vector3();
        [SerializeField] private Vector3 openRight = new Vector3();

        private Vector3 closedLeft;
        private Vector3 closedRight;

        [SyncVar(hook = "OnDoorStateChange")]
        private bool openState; // Server Only

        private int playersInTrigger; // Server Only
        private Coroutine closeTimer; // Server Only
        // Used for ensuring correct timing on certain actions
        private DateTime lastInteraction; // Server Only

        private float animTime; // Client Only
        private new Coroutine animation; // Client Only

        // Interaction stuff
        
        public InteractionEvent Event { get; set; }
        public string Name => openState ? "Close Door" : "Open Door";
        public bool CanInteract() => true;
        public void Interact() {
            openState = !openState;

            if (closeTimer != null) // Reset previous timer
                StopCoroutine(closeTimer);

            if (openState == true) { // If we are now open, we need to start the timer to close again
                // Wait an additional amount of time for the door to open before closing again
                float timeLeftInCurrentInteraction = Math.Min((float)(DateTime.Now - lastInteraction).TotalSeconds, DOOR_TRANSITION_TIME);
                closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME + timeLeftInCurrentInteraction));
            }

            lastInteraction = DateTime.Now;
        }

        // Overriding (non-interesting) methods

        public void OnValidate()
        {
            if (doorTriggerLayers == -1)
                doorTriggerLayers = LayerMask.NameToLayer("Player");
        }

        public void Awake()
        {
            closedLeft = leftPanel.localPosition;
            closedRight = rightPanel.localPosition;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        
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
                (open ? openSfx : closeSfx).Play();

                // Stop any previous animations
                if (animation != null)
                    StopCoroutine(animation);

                animation = StartCoroutine(RunDoorAnim(open));
            }
        }

        /*
         * Runs the animation.
         * TODO: Replace this with a real animation.
         */
        private IEnumerator RunDoorAnim(bool open)
        {
            // 1.0f = open, 0.0f = closed.
            while (animTime <= 1.0f && animTime >= 0.0f) {
                animTime = animTime + (open ? Time.deltaTime : -Time.deltaTime) / DOOR_TRANSITION_TIME;

                leftPanel.localPosition = Vector3.Lerp(closedLeft, openLeft, animTime);
                rightPanel.localPosition = Vector3.Lerp(closedRight, openRight, animTime);

                yield return new WaitForEndOfFrame();
            }
            animTime = open ? 1.0f : 0.0f;

            leftPanel.localPosition = open ? openLeft : closedLeft;
            rightPanel.localPosition = open ? openRight : closedRight;

            // Now that the animation is over, clear the animation Coroutine.
            animation = null;
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            openState = false;
        }
    }
}
