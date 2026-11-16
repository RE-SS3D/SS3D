using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Selection;
using UnityEngine;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Shared discovery helpers for client menus/RPC revalidation — extracted from
    /// <see cref="InteractionController"/> (TECH_DEBT 1.9 / interaction-controller-decomposition).
    /// </summary>
    public static class InteractionDiscovery
    {
        /// <summary>Reused by <see cref="CollectTargetsInto"/> — main-thread only, not re-entrant.</summary>
        private static readonly List<IInteractionTarget> ComponentsScratch = new(8);

        public static IInteractionSource GetActiveInteractionSource(Component host)
        {
            if (host == null)
            {
                return null;
            }

            IHandsController handsController = host.GetComponent<IHandsController>();
            return handsController?.GetActiveInteractionSource();
        }

        public static List<IInteractionTarget> GetTargetsFromGameObject(IInteractionSource source, GameObject targetGameObject)
        {
            List<IInteractionTarget> targets = new();
            CollectTargetsInto(source, targetGameObject, targets);
            return targets;
        }

        public static void CollectTargetsInto(
            IInteractionSource source,
            GameObject targetGameObject,
            List<IInteractionTarget> targets)
        {
            targets.Clear();

            // List overload avoids allocating a new array each GetComponents call.
            ComponentsScratch.Clear();
            targetGameObject.GetComponents(ComponentsScratch);
            for (int i = 0; i < ComponentsScratch.Count; i++)
            {
                IInteractionTarget target = ComponentsScratch[i];
                if ((target as MonoBehaviour)?.enabled == false)
                {
                    continue;
                }

                if (!source.CanInteractWithTarget(target))
                {
                    continue;
                }

                targets.Add(target);
            }

            if (targets.Count < 1)
            {
                // Rare (no IInteractionTarget on selectable). New instance — do not reuse a
                // static fallback; InteractionEntry may hold Target across frames.
                targets.Add(new InteractionTargetGameObject(targetGameObject));
            }
        }

        /// <summary>
        /// Gets all possible interactions from the shader selection pick on the client.
        /// </summary>
        public static List<InteractionEntry> GetViableInteractionsFromSelection(
            IInteractionSource source,
            Selectable current,
            Camera camera,
            IntentType intent,
            out InteractionEvent interactionEvent)
        {
            if (source == null || current == null)
            {
                interactionEvent = null;
                return new List<InteractionEntry>();
            }

            bool hasPoint = SelectionTargetUtility.TryResolveInteractionPoint(camera, current, out Vector3 point, out Vector3 normal);
            return GetViableInteractionsFromTarget(source, current.gameObject, hasPoint, point, normal, intent, out interactionEvent);
        }

        /// <summary>
        /// Gets all possible interactions for a resolved target object and interaction point.
        /// </summary>
        public static List<InteractionEntry> GetViableInteractionsFromTarget(
            IInteractionSource source,
            GameObject targetGameObject,
            bool hasPoint,
            Vector3 point,
            Vector3 normal,
            IntentType intent,
            out InteractionEvent interactionEvent)
        {
            if (source == null)
            {
                interactionEvent = null;
                return new List<InteractionEntry>();
            }

            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, targetGameObject);
            interactionEvent = hasPoint
                ? new InteractionEvent(source, targets[0], point, normal)
                : new InteractionEvent(source, targets[0]);

            return InteractionPipeline.GetViableInteractions(source, targets, interactionEvent, intent);
        }

        /// <summary>
        /// RPC path: point was chosen on the client and sent over the wire (always treated as resolved).
        /// </summary>
        public static List<InteractionEntry> GetViableInteractionsFromTarget(
            IInteractionSource source,
            GameObject targetGameObject,
            Vector3 point,
            IntentType intent,
            out InteractionEvent interactionEvent)
        {
            return GetViableInteractionsFromTarget(source, targetGameObject, hasPoint: true, point, Vector3.zero, intent, out interactionEvent);
        }
    }
}
