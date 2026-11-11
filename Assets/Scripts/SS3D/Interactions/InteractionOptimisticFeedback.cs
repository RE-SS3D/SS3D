using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Starts the progress ring on click, before the server confirms, so a timed step shows at once.
    /// <see cref="ClientDelayedInteraction"/> adopts the ring when the confirm arrives; a rejection clears it.
    /// </summary>
    public static class InteractionOptimisticFeedback
    {
        public static void TryBeginDelayed(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (interaction is not DelayedInteraction delayedInteraction || delayedInteraction.ClientDelay < 0.1f)
            {
                return;
            }

            // Steps with their own client feedback (melee telegraph, paperdoll spinner) create no client.
            if (delayedInteraction.CreateClient(interactionEvent) == null)
            {
                return;
            }

            if (!DelayedProgressFeedback.TryGetKey(interactionEvent, out int key, out Transform source))
            {
                return;
            }

            DelayedProgressFeedback.ResolveAnchor(interactionEvent, source, out Transform anchor, out Vector3 point);
            DelayedProgressFeedback.Begin(key, anchor, point, delayedInteraction.ClientDelay);
        }

        /// <summary>Removes the local player's rings without a message (server rejection).</summary>
        public static void ClearAll() => DelayedProgressFeedback.ClearAll();

        /// <summary>Shows "Cancelled" on the local player's running rings (Cancel key).</summary>
        public static void CancelLocal() => DelayedProgressFeedback.CancelAll(DelayedCancelReason.Manual);
    }
}
