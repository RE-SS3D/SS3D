using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// An entry of an interaction and its target interactable.
    /// </summary>
    public struct InteractionEntry
    {
        public readonly IInteractionTarget Target;
        public readonly IInteraction Interaction;
        public readonly InteractionIdentifier Id;

        public InteractionEntry(IInteractionTarget target, IInteraction interaction, int targetComponentIndex)
        {
            Target = target;
            Interaction = interaction;
            Id = new InteractionIdentifier(interaction.GetGenericName(), targetComponentIndex);
        }

        /// <summary>
        /// Creates a provisional entry. Callers that discover interactions should rebuild indices before sending them over the network.
        /// </summary>
        public InteractionEntry(IInteractionTarget target, IInteraction interaction)
            : this(target, interaction, InteractionIdentifier.SyntheticTargetIndex)
        {
        }

        /// <summary>
        /// Creates an entry with a resolved target component index for network identification.
        /// </summary>
        public static InteractionEntry Create(IInteractionTarget target, IInteraction interaction, GameObject targetGameObject)
        {
            return new InteractionEntry(target, interaction, ResolveTargetComponentIndex(target, targetGameObject));
        }

        public static int ResolveTargetComponentIndex(IInteractionTarget target, GameObject targetGameObject)
        {
            if (target == null)
            {
                return InteractionIdentifier.SourceOnlyTargetIndex;
            }

            if (target is InteractionTargetGameObject)
            {
                return InteractionIdentifier.SyntheticTargetIndex;
            }

            if (targetGameObject == null)
            {
                return InteractionIdentifier.SyntheticTargetIndex;
            }

            IInteractionTarget[] components = targetGameObject.GetComponents<IInteractionTarget>();

            for (int i = 0; i < components.Length; i++)
            {
                if (ReferenceEquals(components[i], target))
                {
                    return i;
                }
            }

            return InteractionIdentifier.SyntheticTargetIndex;
        }

        public static bool TryResolve(
            System.Collections.Generic.List<InteractionEntry> entries,
            InteractionIdentifier id,
            out InteractionEntry entry)
        {
            foreach (InteractionEntry candidate in entries)
            {
                if (candidate.Id == id)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
