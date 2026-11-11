using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Client side of a <see cref="DelayedInteraction"/>: drives the progress ring over the target
    /// (<see cref="DelayedProgressFeedback"/>), shows done when the timer runs out and the server's
    /// cancel reason when it stops early.
    /// </summary>
    public sealed class ClientDelayedInteraction : IClientInteraction, IReasonedClientCancel
    {
        /// <summary>
        /// How long the instance outlives the timer. A completion-time refusal (missing materials) arrives
        /// as a cancel just after the ring reads done; keep listening so it can replace done.
        /// </summary>
        private const float CancelGraceSeconds = 1f;

        private int _ringId = DelayedProgressFeedback.NoRing;
        private float _startTime;
        private bool _completed;

        /// <summary>
        /// The duration of the step in seconds
        /// </summary>
        public float Delay { get; set; }

        /// <inheritdoc />
        public bool ClientStart(InteractionEvent interactionEvent)
        {
            _startTime = Time.time;

            if (Delay < 0.1f || !DelayedProgressFeedback.TryGetKey(interactionEvent, out int key, out Transform source))
            {
                return true;
            }

            // The click already started an optimistic ring; keep it rather than restarting the sweep.
            if (DelayedProgressFeedback.TryGetRunning(key, out _ringId))
            {
                return true;
            }

            DelayedProgressFeedback.ResolveAnchor(interactionEvent, source, out Transform anchor, out Vector3 point);
            _ringId = DelayedProgressFeedback.Begin(key, anchor, point, Delay);
            return true;
        }

        /// <inheritdoc />
        public bool ClientUpdate(InteractionEvent interactionEvent)
        {
            float elapsed = Time.time - _startTime;
            if (!_completed && elapsed >= Delay)
            {
                _completed = true;
                DelayedProgressFeedback.Complete(_ringId);
            }

            return elapsed < Delay + CancelGraceSeconds;
        }

        /// <inheritdoc />
        public void ClientCancel(InteractionEvent interactionEvent) => ClientCancel(interactionEvent, DelayedCancelReason.Interrupted);

        /// <inheritdoc />
        public void ClientCancel(InteractionEvent interactionEvent, DelayedCancelReason reason)
        {
            DelayedProgressFeedback.Cancel(_ringId, reason);
        }
    }
}
