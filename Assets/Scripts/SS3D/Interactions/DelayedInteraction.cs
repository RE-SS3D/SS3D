using System;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Base class for interactions which execute after a delay
    /// </summary>
    public abstract class DelayedInteraction : IDelayedInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;

        private float _startTime;
        private float _lastCheck;
        private Vector3 _startPosition;
        
        /// <summary>
        /// The delay in seconds before performing the interaction
        /// </summary>
        protected float Delay { get; init; }

        public float ClientDelay => Delay;

        protected bool HasStarted { get; private set; }

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

        public abstract string GetName(InteractionEvent interactionEvent);

        public abstract string GetGenericName();

        public virtual Sprite GetIcon(InteractionEvent interactionEvent) => Icon;

        public abstract bool CanInteract(InteractionEvent interactionEvent);

        /// <summary>
        /// Sets up the delay
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            CaptureStartPosition(interactionEvent);
            StartCounter();

            return true;
        }

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public virtual bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (HasStarted
                && interactionEvent.Source.GetRootSource() is IGameObjectProvider provider
                && !InteractionExtensions.CharacterMoveCheck(_startPosition, provider.GameObject.transform.position))
            {
                interactionEvent.Source.CancelInteraction(reference);
                return true;
            }

            if (_lastCheck + CheckInterval < Time.time && HasStarted)
            {
                if (!CanInteract(interactionEvent) || !interactionEvent.Source.CanContinueInteraction())
                {
                    // Cancel the interaction
                    interactionEvent.Source.CancelInteraction(reference);

                    return true;
                }

                _lastCheck = Time.time;
            }

            if (_startTime + Delay < Time.time && HasStarted)
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

        /// <inheritdoc />
        public abstract void Cancel(InteractionEvent interactionEvent, InteractionReference reference);

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        protected abstract void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference);

        protected void CaptureStartPosition(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source.GetRootSource() is IGameObjectProvider provider)
            {
                _startPosition = provider.GameObject.transform.position;
            }
        }

        protected void StartCounter()
        {
            _startTime = Time.time;
            _lastCheck = _startTime;
            HasStarted = true;
        }
    }
}