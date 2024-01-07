using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCraft : MonoBehaviour, IInteractionSourceExtension
{
        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            HandCraftInteraction interaction = new HandCraftInteraction(1f, GetComponent<InteractionSource>().transform);

            foreach (IInteractionTarget target in targets)
            {
                interactions.Add(new InteractionEntry(target, interaction));
            }
        }
}
