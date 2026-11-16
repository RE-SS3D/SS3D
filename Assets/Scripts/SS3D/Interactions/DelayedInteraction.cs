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
        /// When true, moving farther than CharacterMoveCheck tolerance cancels the windup.
        /// Melee overrides to false so walking does not abort connect/recovery.
        /// </summary>
        protected virtual bool CancelOnMove => true;

        /// <summary>
        /// When true, damage to the body holding the source cancels the windup (construction.md §3, D7 of
        /// 2026-11_construction-mvp1). Defuse keeps the default.
        /// </summary>
        public virtual bool CancelOnDamage => true;

        private IDamageNotifier _damageNotifier;
        private bool _damagedSinceStart;

        /// <summary>
        /// The interval in seconds in which CanInteract is checked
        /// </summary>
        protected float CheckInterval { get; set; }


        /// <summary>
        /// Creates a client-side interaction object for this interaction
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        public virtual IClientInteraction CreateClient(InteractionEvent interactionEvent)
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
            SubscribeDamage(interactionEvent);

            return true;
        }

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="reference">The reference to this interaction</param>
        public virtual bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (HasStarted && CancelOnDamage && _damagedSinceStart)
            {
                return CancelWith(interactionEvent, reference, DelayedCancelReason.Hurt);
            }

            if (HasStarted
                && CancelOnMove
                && !InteractionExtensions.CharacterMoveCheck(
                    _startPosition,
                    ResolveMoveCheckPosition(interactionEvent)))
            {
                return CancelWith(interactionEvent, reference, DelayedCancelReason.Moved);
            }

            if (_lastCheck + CheckInterval < Time.time && HasStarted)
            {
                if (!interactionEvent.Source.CanContinueInteraction())
                {
                    return CancelWith(interactionEvent, reference, DelayedCancelReason.KnockedDown);
                }

                if (!CanInteract(interactionEvent))
                {
                    return CancelWith(interactionEvent, reference, ResolveCannotInteractReason(interactionEvent));
                }

                _lastCheck = Time.time;
            }

            if (_startTime + Delay < Time.time && HasStarted)
            {
                if (CanInteract(interactionEvent))
                {
                    ReleaseCancelHooks();
                    StartDelayed(interactionEvent, reference);

                    return false;
                }

                // Cancel own interaction
                return CancelWith(interactionEvent, reference, ResolveCannotInteractReason(interactionEvent));
            }

            return true;
        }

        /// <summary>
        /// Reason sent when <see cref="CanInteract"/> turns false mid-step. Construction steps report
        /// missing materials; everything else is a generic interruption.
        /// </summary>
        protected virtual DelayedCancelReason ResolveCannotInteractReason(InteractionEvent interactionEvent) =>
            DelayedCancelReason.Interrupted;

        /// <summary>
        /// Drops the damage subscription. <see cref="InteractionSource"/> calls this on every cancel path,
        /// including the Cancel key, so a cancelled step does not keep listening to the body.
        /// </summary>
        public void ReleaseCancelHooks()
        {
            if (_damageNotifier != null)
            {
                _damageNotifier.ServerDamaged -= HandleServerDamaged;
                _damageNotifier = null;
            }
        }

        private bool CancelWith(InteractionEvent interactionEvent, InteractionReference reference, DelayedCancelReason reason)
        {
            ReleaseCancelHooks();
            interactionEvent.Source.CancelInteraction(reference, reason);
            return true;
        }

        private void SubscribeDamage(InteractionEvent interactionEvent)
        {
            ReleaseCancelHooks();
            _damagedSinceStart = false;

            if (!CancelOnDamage || interactionEvent.Source.GetRootSource() is not IGameObjectProvider provider
                || provider.GameObject == null)
            {
                return;
            }

            _damageNotifier = provider.GameObject.GetComponentInParent<IDamageNotifier>();
            if (_damageNotifier != null)
            {
                _damageNotifier.ServerDamaged += HandleServerDamaged;
            }
        }

        private void HandleServerDamaged() => _damagedSinceStart = true;

        /// <inheritdoc />
        public abstract void Cancel(InteractionEvent interactionEvent, InteractionReference reference);

        /// <summary>
        /// Starts the interaction after the delay has passed
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        protected abstract void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference);

        /// <summary>
        /// World position used for cancel-on-move. Override when the source transform is a moving bone
        /// (e.g. hand during a swing anim) so the character root is checked instead.
        /// </summary>
        protected virtual Vector3 ResolveMoveCheckPosition(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source.GetRootSource() is IGameObjectProvider provider)
            {
                return provider.GameObject.transform.position;
            }

            return _startPosition;
        }

        protected void CaptureStartPosition(InteractionEvent interactionEvent)
        {
            _startPosition = ResolveMoveCheckPosition(interactionEvent);
        }

        protected void StartCounter()
        {
            _startTime = Time.time;
            _lastCheck = _startTime;
            HasStarted = true;
        }
    }
}