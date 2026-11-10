using System.Collections.Generic;
using SS3D.Interactions.Interfaces;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Shared discovery and filtering for client interaction menus and server RPC re-validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Discover</b> produces candidates: target-bound entries (<c>Target != null</c>) and optional
    /// source-only entries (<c>Target == null</c>, e.g. Drop). Discover may pre-filter; it does not
    /// mean “already viable for the menu.”
    /// </para>
    /// <para>
    /// <b>FilterAndSort</b> is the sole full viability gate for menus/RPC
    /// (<see cref="IInteraction.CanInteract"/>, intent, <see cref="IInteractionSource.CanExecuteInteraction"/>).
    /// </para>
    /// <para>
    /// Hover outlines use <see cref="TryEvaluateOutlineInteractability"/> (target-bound only).
    /// </para>
    /// </remarks>
    public static class InteractionPipeline
    {
        private static readonly ProfilerMarker DiscoverPerformanceMarker = new("SS3D.Interactions.Discover");

        public static List<InteractionEntry> GetViableInteractions(
            IInteractionSource source,
            List<IInteractionTarget> targets,
            InteractionEvent interactionEvent,
            IntentType intent = IntentType.Help)
        {
            List<InteractionEntry> discovered = Discover(source, targets, interactionEvent);

            return FilterAndSort(source, discovered, interactionEvent, intent);
        }

        public static List<InteractionEntry> Discover(
            IInteractionSource source,
            List<IInteractionTarget> targets,
            InteractionEvent interactionEvent)
        {
            using (DiscoverPerformanceMarker.Auto())
            {
                List<InteractionEntry> interactions = new();
                GameObject targetGameObject = ResolveTargetGameObject(targets);

                foreach (IInteractionTarget target in targets)
                {
                    InteractionEvent e = interactionEvent.WithTarget(target);
                    IInteraction[] targetInteractions = target.CreateTargetInteractions(e);

                    foreach (IInteraction interaction in targetInteractions)
                    {
                        interactions.Add(InteractionEntry.Create(target, interaction, targetGameObject));
                    }
                }

                source.CreateSourceInteractions(targets.ToArray(), interactions, interactionEvent);
                RebuildEntryIndices(interactions, targetGameObject);

                return interactions;
            }
        }

        /// <summary>
        /// Hover-outline probe: target-bound interactions only (no source-only Drop/etc.), no sort,
        /// no wire-index rebuild. For LateUpdate feedback — not RPC dispatch.
        /// </summary>
        /// <returns>
        /// False when nothing target-bound was discovered (caller should hide outline).
        /// True when at least one target-bound interaction exists; <paramref name="hasViableInteractions"/>
        /// distinguishes green vs yellow outline.
        /// </returns>
        public static bool TryEvaluateOutlineInteractability(
            IInteractionSource source,
            List<IInteractionTarget> targets,
            InteractionEvent interactionEvent,
            IntentType intent,
            out bool hasViableInteractions)
        {
            hasViableInteractions = false;
            bool hasTargetBound = false;

            if (source == null || targets == null || targets.Count == 0 || interactionEvent == null)
            {
                return false;
            }

            IInteractionTarget previousTarget = interactionEvent.Target;

            foreach (IInteractionTarget target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                // Mutate Target in place — WithTarget allocates a new InteractionEvent every call.
                interactionEvent.Target = target;
                IInteraction[] targetInteractions = target.CreateTargetInteractions(interactionEvent);

                foreach (IInteraction interaction in targetInteractions)
                {
                    hasTargetBound = true;

                    if (hasViableInteractions)
                    {
                        continue;
                    }

                    if (!interaction.CanInteract(interactionEvent))
                    {
                        continue;
                    }

                    if (!MatchesIntent(interaction, intent))
                    {
                        continue;
                    }

                    if (!source.CanExecuteInteraction(interaction))
                    {
                        continue;
                    }

                    hasViableInteractions = true;
                }

                if (hasTargetBound && hasViableInteractions)
                {
                    interactionEvent.Target = previousTarget;
                    return true;
                }
            }

            interactionEvent.Target = previousTarget;
            return hasTargetBound;
        }

        /// <summary>
        /// Legacy overload for callers that only have a point/normal. Prefer the
        /// <see cref="InteractionEvent"/> overload so unresolved hits keep <see cref="InteractionEvent.HasPoint"/> false.
        /// </summary>
        public static bool TryEvaluateOutlineInteractability(
            IInteractionSource source,
            List<IInteractionTarget> targets,
            Vector3 point,
            Vector3 normal,
            IntentType intent,
            out bool hasViableInteractions)
        {
            if (source == null)
            {
                hasViableInteractions = false;
                return false;
            }

            InteractionEvent interactionEvent = new(source, null, point, normal);
            return TryEvaluateOutlineInteractability(source, targets, interactionEvent, intent, out hasViableInteractions);
        }

        /// <summary>Why <see cref="ExplainNotViable"/> found a requested interaction not viable.</summary>
        public enum ViabilityFailure
        {
            None,
            NoSuchInteraction,
            CannotInteract,
            WrongIntent,
            SourceRefused,
        }

        /// <summary>
        /// Diagnostics (tech-debt P1.9, #192): which <see cref="FilterAndSort"/> check refuses the entry with
        /// <paramref name="id"/> among the <paramref name="discovered"/> (unfiltered) entries. <see cref="ViabilityFailure.None"/>
        /// when it would pass. Not for gameplay decisions.
        /// </summary>
        public static ViabilityFailure ExplainNotViable(
            IInteractionSource source,
            List<InteractionEntry> discovered,
            InteractionEvent interactionEvent,
            IntentType intent,
            InteractionIdentifier id)
        {
            foreach (InteractionEntry entry in discovered)
            {
                if (entry.Id != id)
                {
                    continue;
                }

                if (!entry.Interaction.CanInteract(interactionEvent.WithTarget(entry.Target)))
                {
                    return ViabilityFailure.CannotInteract;
                }

                if (!MatchesIntent(entry.Interaction, intent))
                {
                    return ViabilityFailure.WrongIntent;
                }

                return source.CanExecuteInteraction(entry.Interaction) ? ViabilityFailure.None : ViabilityFailure.SourceRefused;
            }

            return ViabilityFailure.NoSuchInteraction;
        }

        public static List<InteractionEntry> FilterAndSort(
            IInteractionSource source,
            List<InteractionEntry> entries,
            InteractionEvent interactionEvent,
            IntentType intent = IntentType.Help)
        {
            List<InteractionEntry> viable = new();

            foreach (InteractionEntry entry in entries)
            {
                InteractionEvent e = interactionEvent.WithTarget(entry.Target);

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
        /// Legacy overload. Point/normal always set <see cref="InteractionEvent.HasPoint"/> true.
        /// Prefer the <see cref="InteractionEvent"/> overload.
        /// </summary>
        public static List<InteractionEntry> FilterAndSort(
            IInteractionSource source,
            List<InteractionEntry> entries,
            Vector3 point,
            Vector3 normal = default,
            IntentType intent = IntentType.Help)
        {
            InteractionEvent interactionEvent = new(source, null, point, normal);
            return FilterAndSort(source, entries, interactionEvent, intent);
        }

        /// <summary>
        /// Hover outlines only reflect interactions that target the hovered object.
        /// Source-only entries (e.g. Drop) must not light up every Selectable under the cursor.
        /// </summary>
        public static List<InteractionEntry> FilterForOutline(List<InteractionEntry> entries)
        {
            List<InteractionEntry> targeted = new();

            foreach (InteractionEntry entry in entries)
            {
                if (entry.IsSourceOnly)
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

            // Unrestricted world verbs (Drop, Open, MI, …) are Help-default.
            // Harm (and other exclusive modes) must opt in via IIntentRestrictedInteraction.
            return intent == IntentType.Help;
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
