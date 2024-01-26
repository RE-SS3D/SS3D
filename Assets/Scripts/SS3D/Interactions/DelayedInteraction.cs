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
        /// The interval in seconds in which CanInteract is checked
        /// </summary>
        protected float CheckInterval { get; set; }

        private bool _hasStarted;

        private float _startTime;
        private float _lastCheck;

        public bool HasStarted => _hasStarted;

        /// <summary>
        /// Creates a client-side interaction object for this interaction
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            // Don't create client interaction if delay too small
            if (Math.Abs(Delay) < 0.1f)
            {
                return null;
            }

            return new ClientDelayedInteraction
            {
                Delay = Delay
            };
        }

        public abstract override string GetName(InteractionEvent interactionEvent);
        public override Sprite GetIcon(InteractionEvent interactionEvent) { return Icon; }
        public abstract override bool CanInteract(InteractionEvent interactionEvent);

        /// <summary>
        /// Sets up the delay
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            StartCounter();
            return true;
        }

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (_lastCheck + CheckInterval < Time.time)
            {
                if (!CanInteract(interactionEvent))
                {
                    // Cancel the interaction
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

        protected void StartCounter()
        {
            _startTime = Time.time;
            _lastCheck = _startTime;
            _hasStarted = true;
        }

        /// <inheritdoc />
        public abstract override void Cancel(InteractionEvent interactionEvent, InteractionReference reference);

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        protected abstract void StartDelayed(InteractionEvent interactionEvent);
    }
}