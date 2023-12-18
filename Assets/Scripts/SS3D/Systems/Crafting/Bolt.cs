using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class Bolt : MonoBehaviour, IInteractionSourceExtension
    {
        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            BoltInteraction interaction = new BoltInteraction(1f, GetComponent<InteractionSource>().transform);

            foreach (IInteractionTarget target in targets)
            {
                interactions.Add(new InteractionEntry(target, interaction));
            }
        }
    }
}
