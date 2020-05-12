using Mirror;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// Base class for interactions which occur after a delay
    /// </summary>
    public abstract class DelayedInteraction : /* Hack (no multiple inheritance) */ NetworkBehaviour, ContinuousInteraction
    {
        /// <summary>
        /// The delay in seconds before the interaction is performed
        /// </summary>
        public float Delay;
        private float startTime;

        /// <summary>
        /// The prefab to use as a loading bar. If this value is null no bar is shown.
        /// </summary>
        public GameObject LoadingBarPrefab;
        
        public abstract InteractionEvent Event { get; set; }
        public abstract string Name { get; }

        public virtual bool CanInteract()
        {
            // Prevents multiple people from interacting at the same time
            // Is this a good idea?
            return startTime == 0;
        }

        public void Interact()
        {
            startTime = Time.time;
            TargetStartInteraction(Event.connectionToClient, Delay);
        }

        public bool ContinueInteracting()
        {
            // Delay has been reached
            if (startTime + Delay < Time.time)
            {
                // Perform the actual interaction
                InteractDelayed();
                startTime = 0;
                return false;
            }

            return true;
        }

        public void EndInteraction()
        {
            // If start time is not zero, the interaction was interrupted
            if (startTime != 0)
            {
                startTime = 0;
                // Notify the client of the interruption
                TargetEndInteraction(Event.connectionToClient);
            }
        }

        [TargetRpc]
        public void TargetStartInteraction(NetworkConnection target, float delay)
        {
            if (LoadingBarPrefab != null)
            {
                // Create loading bar in scene
                GameObject loadingBar = Instantiate(LoadingBarPrefab, transform);
                // TODO: Set position above object
                loadingBar.transform.localPosition = Vector3.zero;
                LoadingBar bar = loadingBar.GetComponent<LoadingBar>();
                Assert.IsNotNull(bar, "Loading bar prefab has no LoadingBar component");
                // Set loading bar delay
                bar.Duration = delay;
            }
        }

        [TargetRpc]
        public void TargetEndInteraction(NetworkConnection target)
        {
            Destroy(GetComponentInChildren<LoadingBar>().gameObject);
        }
        
        /// <summary>
        /// The method which gets called after the delay
        /// </summary>
        protected abstract void InteractDelayed();
    }
}