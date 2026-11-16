using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    /// <summary>
    /// Discovers <see cref="SearchInteraction"/> from hand sources when hovering another character.
    /// </summary>
    public sealed class HandSearchExtension : MonoBehaviour, IInteractionSourceExtension
    {
        public void GetSourceInteractions(
            IInteractionTarget[] targets,
            List<InteractionEntry> interactions,
            InteractionEvent context)
        {
            SearchInteraction search = SearchInteraction.Instance;
            for (int i = 0; i < targets.Length; i++)
            {
                IInteractionTarget target = targets[i];
                if (search.CanInteract(context.WithTarget(target)))
                {
                    interactions.Add(new InteractionEntry(target, search));
                }
            }
        }
    }
}
