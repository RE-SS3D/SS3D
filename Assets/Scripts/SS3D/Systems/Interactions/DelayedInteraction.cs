using System;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Base class for interactions which execute after a delay
    /// </summary>
    public abstract class DelayedInteraction : IInteraction
    {
        private bool _hasStarted;

        private float _startTime;

        private float _lastCheck;

        /// <summary>
        /// The delay in seconds before performing the interaction
        /// </summary>
        public float Delay { get; set; }

        public bool HasStarted => _hasStarted;


        /// <summary>
        /// The interval in seconds in which CanInteract is checked
        /// </summary>
        protected float CheckInterval { get; set; }

        /// <summary>
        /// Creates a client-side interaction object for this interaction
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            // Don't create client interaction if delay too small
            if (Math.Abs(Delay) < 0.1f)
            {
                return null;
            }

            return new ClientDelayedInteraction
            {
                Delay = Delay,
            };
        }

        /// <summary>
        /// Sets up the delay
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            StartCounter();
            return StartImmediately(interactionEvent, reference);
        }

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (_lastCheck + CheckInterval < Time.time && _hasStarted)
            {
                if (!CanInteract(interactionEvent))
                {
                    // Cancel the interaction
                    interactionEvent.Source.CancelInteraction(reference);
                    return true;
                }

                _lastCheck = Time.time;
            }

            if (_startTime + Delay < Time.time && _hasStarted)
            {
                if (CanInteract(interactionEvent))
                {
                    StartDelayed(interactionEvent, reference);
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

        public abstract string GetName(InteractionEvent interactionEvent);

        public abstract string GetGenericName();

        public abstract InteractionType InteractionType { get; }

        public abstract Sprite GetIcon(InteractionEvent interactionEvent);

        public abstract bool CanInteract(InteractionEvent interactionEvent);

        /// <inheritdoc />
        public abstract void Cancel(InteractionEvent interactionEvent, InteractionReference reference);

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        protected abstract void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference);

        /// <summary>
        /// first step of a delayed interaction.
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        protected abstract bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference);

        private void StartCounter()
        {
            _startTime = Time.time;
            _lastCheck = _startTime;
            _hasStarted = true;
        }
    }
}
