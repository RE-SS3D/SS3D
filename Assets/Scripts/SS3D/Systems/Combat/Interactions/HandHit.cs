using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Little script to add next to Hand script, allowing to add a HitInteraction from hand sources.
    /// </summary>
    public class HandHit : MonoBehaviour, IInteractionSourceExtension
    {
        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            HitInteraction interaction = new HitInteraction(0.3f);

            foreach (IInteractionTarget target in targets)
            {
                interactions.Add(new(target, interaction));
            }
        }
    }
}
