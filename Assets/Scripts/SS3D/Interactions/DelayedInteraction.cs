using System;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Base class for interactions which execute after a delay
    /// </summary>
    public abstract class DelayedInteraction : Interaction
    {
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

        private float _startTime;
        private float _lastCheck;

        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
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

        public abstract override string GetName(InteractionEvent interactionEvent);
        public override Sprite GetIcon(InteractionEvent interactionEvent) { return Icon; }
        public abstract override bool CanInteract(InteractionEvent interactionEvent);

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            _startTime = Time.time;
            _lastCheck = _startTime;
            return true;
        }

        public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (_lastCheck + CheckInterval < Time.time)
            {
                if (!CanInteract(interactionEvent))
                {
                    // Cancel own interaction
                    interactionEvent.Source.CancelInteraction(reference);
                    return true;
                }

                _lastCheck = Time.time;
            }

            if (_startTime + Delay < Time.time)
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

        public abstract override void Cancel(InteractionEvent interactionEvent, InteractionReference reference);

        protected abstract void StartDelayed(InteractionEvent interactionEvent);
    }
}