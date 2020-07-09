using System;
using UnityEngine;

namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// Base class for interactions which execute after a delay
    /// </summary>
    public abstract class DelayedInteraction : IInteraction
    {
        public Sprite icon;
        /// <summary>
        /// The delay in seconds before performing the interaction
        /// </summary>
        public float Delay { get; set; }
        /// <summary>
        /// The loading bar prefab to use on the client
        /// </summary>
        public GameObject LoadingBarPrefab { get; set; }
        /// <summary>
        /// The interval in seconds in which CanInteract is checked
        /// </summary>
        protected float CheckInterval { get; set; }
        private float startTime;
        private float lastCheck;
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            // Don't create client interaction if delay too small
            if (Math.Abs(Delay) < 0.1f)
            {
                return null;
            }
            
            return new ClientDelayedInteraction
            {
                Delay = Delay, LoadingBarPrefab = LoadingBarPrefab
            };
        }

        public abstract string GetName(InteractionEvent interactionEvent);

        public virtual Sprite GetIcon(InteractionEvent interactionEvent) { return icon; }
        public abstract bool CanInteract(InteractionEvent interactionEvent);

        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            startTime = Time.time;
            lastCheck = startTime;
            return true;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (lastCheck + CheckInterval < Time.time)
            {
                if (!CanInteract(interactionEvent))
                {
                    // Cancel own interaction
                    interactionEvent.Source.CancelInteraction(reference);
                    return true;
                }

                lastCheck = Time.time;
            }
            
            if (startTime + Delay < Time.time)
            {
                if (CanInteract(interactionEvent))
                {
                    StartDelayed(interactionEvent);
                    return false;
                }
                else
                {
                    // Cancel own interaction
                    interactionEvent.Source.CancelInteraction(reference);
                    return true;
                }
            }

            return true;
        }

        public abstract void Cancel(InteractionEvent interactionEvent, InteractionReference reference);

        protected abstract void StartDelayed(InteractionEvent interactionEvent);
    }
}