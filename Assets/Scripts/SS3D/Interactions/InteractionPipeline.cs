using System.Collections.Generic;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Shared discovery and filtering for client interaction menus and server RPC re-validation.
    /// </summary>
    public static class InteractionPipeline
    {
        public static List<InteractionEntry> GetViableInteractions(
            IInteractionSource source,
            List<IInteractionTarget> targets,
            InteractionEvent interactionEvent,
            IntentType intent = IntentType.Help)
        {
            List<InteractionEntry> discovered = Discover(source, targets, interactionEvent);

            return FilterAndSort(source, discovered, interactionEvent.Point, interactionEvent.Normal, intent);
        }

        public static List<InteractionEntry> Discover(
            IInteractionSource source,
            List<IInteractionTarget> targets,
            InteractionEvent interactionEvent)
        {
            List<InteractionEntry> interactions = new();
            Vector3 point = interactionEvent.Point;
            GameObject targetGameObject = ResolveTargetGameObject(targets);

            foreach (IInteractionTarget target in targets)
            {
                InteractionEvent e = new(source, target, point, interactionEvent.Normal);
                IInteraction[] targetInteractions = target.CreateTargetInteractions(e);

                foreach (IInteraction interaction in targetInteractions)
                {
                    interactions.Add(InteractionEntry.Create(target, interaction, targetGameObject));
                }
            }

            source.CreateSourceInteractions(targets.ToArray(), interactions);
            RebuildEntryIndices(interactions, targetGameObject);

            return interactions;
        }

        public static List<InteractionEntry> FilterAndSort(
            IInteractionSource source,
            List<InteractionEntry> entries,
            Vector3 point,
            Vector3 normal = default,
            IntentType intent = IntentType.Help)
        {
            List<InteractionEntry> viable = new();

            foreach (InteractionEntry entry in entries)
            {
                InteractionEvent e = new(source, entry.Target, point, normal);

                if (!entry.Interaction.CanInteract(e))
                {
                    continue;
                }

                if (!MatchesIntent(entry.Interaction, intent))
                {
                    continue;
                }

                if (!source.CanExecuteInteraction(entry.Interaction))
                {
                    continue;
                }

                viable.Add(entry);
            }

            viable.Sort((a, b) => b.Interaction.Priority.CompareTo(a.Interaction.Priority));

            return viable;
        }

        /// <summary>
        /// Hover outlines only reflect interactions that target the hovered object.
        /// Source-only entries (e.g. Drop, which always appears while holding an item) must not
        /// light up every Selectable under the cursor.
        /// </summary>
        public static List<InteractionEntry> FilterForOutline(List<InteractionEntry> entries)
        {
            List<InteractionEntry> targeted = new();

            foreach (InteractionEntry entry in entries)
            {
                if (entry.Target == null)
                {
                    continue;
                }

                targeted.Add(entry);
            }

            return targeted;
        }

        public static bool MatchesIntent(IInteraction interaction, IntentType intent)
        {
            if (interaction is IIntentRestrictedInteraction restricted)
            {
                return restricted.AllowedIntent == intent;
            }

            return true;
        }

        private static GameObject ResolveTargetGameObject(List<IInteractionTarget> targets)
        {
            foreach (IInteractionTarget target in targets)
            {
                if (target is IGameObjectProvider provider)
                {
                    return provider.GameObject;
                }

                if (target is Component component)
                {
                    return component.gameObject;
                }
            }

            return null;
        }

        private static void RebuildEntryIndices(List<InteractionEntry> interactions, GameObject targetGameObject)
        {
            for (int i = 0; i < interactions.Count; i++)
            {
                InteractionEntry entry = interactions[i];
                interactions[i] = InteractionEntry.Create(entry.Target, entry.Interaction, targetGameObject);
            }
        }
    }
}
