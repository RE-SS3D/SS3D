using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Selection;
using UnityEngine;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Shared dispatch/resolve helpers for interaction RPCs — extracted from
    /// <see cref="InteractionController"/> (TECH_DEBT 1.9 / interaction-controller-decomposition).
    /// </summary>
    public static class InteractionDispatch
    {
        public const string ExamineInteractionName = "Examine";

        /// <summary>
        /// Resolves a client-dispatched interaction. Exact <see cref="InteractionIdentifier"/> match first;
        /// falls back to generic name when the client hovered a child Selectable (body part) but the RPC
        /// revalidates against the parent NetworkObject root (different target-component indices).
        /// </summary>
        public static bool TryResolveDispatchedInteraction(
            List<InteractionEntry> viableInteractions,
            InteractionIdentifier id,
            out InteractionEntry interaction)
        {
            if (InteractionEntry.TryResolve(viableInteractions, id, out interaction))
            {
                return true;
            }

            for (int i = 0; i < viableInteractions.Count; i++)
            {
                if (string.Equals(viableInteractions[i].Id.GenericName, id.GenericName, System.StringComparison.Ordinal))
                {
                    interaction = viableInteractions[i];
                    return true;
                }
            }

            interaction = default;
            return false;
        }

        public static bool TryGetNetworkTarget(InteractionEvent interactionEvent, out NetworkObject networkObject)
        {
            networkObject = null;

            if (interactionEvent?.Target == null)
            {
                return false;
            }

            return TryGetNetworkObject(interactionEvent.Target, out networkObject);
        }

        public static bool TryGetNetworkTargetForDispatch(
            InteractionEntry entry,
            InteractionEvent interactionEvent,
            Selectable currentSelectable,
            out NetworkObject networkObject)
        {
            if (TryGetNetworkTarget(interactionEvent, out networkObject))
            {
                return true;
            }

            if (entry.Target != null)
            {
                return false;
            }

            if (currentSelectable == null)
            {
                return false;
            }

            networkObject = currentSelectable.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = currentSelectable.GetComponentInParent<NetworkObject>();
            }

            return networkObject != null;
        }

        public static bool TryGetNetworkObject(IInteractionTarget target, out NetworkObject networkObject)
        {
            networkObject = null;

            GameObject targetGameObject = null;
            if (target is IGameObjectProvider targetProvider)
            {
                targetGameObject = targetProvider.GameObject;
            }
            else if (target is Component targetComponent)
            {
                targetGameObject = targetComponent.gameObject;
            }

            if (targetGameObject == null)
            {
                return false;
            }

            networkObject = targetGameObject.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = targetGameObject.GetComponentInParent<NetworkObject>();
            }

            return networkObject != null;
        }

        public static IInteractionTarget ResolveFallbackTarget(
            InteractionEvent interactionEvent,
            InteractionEntry entry,
            Selectable currentSelectable,
            IInteractionSource source)
        {
            if (entry.Target != null)
            {
                return entry.Target;
            }

            if (interactionEvent?.Target != null)
            {
                return interactionEvent.Target;
            }

            if (currentSelectable == null || source == null)
            {
                return null;
            }

            List<IInteractionTarget> targets = InteractionDiscovery.GetTargetsFromGameObject(source, currentSelectable.gameObject);
            return targets.Count > 0 ? targets[0] : null;
        }

        public static List<InteractionEntry> FilterRadialInteractions(List<InteractionEntry> interactions)
        {
            return interactions
                .Where(entry => entry.Interaction.GetGenericName() != ExamineInteractionName)
                .ToList();
        }

        public static bool TryValidateInteractionTarget(NetworkObject target, out GameObject targetGameObject)
        {
            targetGameObject = null;

            if (target == null || !target.IsSpawned)
            {
                return false;
            }

            targetGameObject = target.gameObject;

            if (targetGameObject.GetComponent<Selectable>() == null && targetGameObject.GetComponentInChildren<Selectable>() == null)
            {
                return false;
            }

            return true;
        }
    }
}
