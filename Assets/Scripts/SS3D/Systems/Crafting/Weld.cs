using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weld : MonoBehaviour, IInteractionSourceExtension
{
    public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
    {
        WeldInteraction interaction = new WeldInteraction(1f, GetComponent<InteractionSource>().transform);

        foreach (IInteractionTarget target in targets)
        {
            interactions.Add(new InteractionEntry(target, interaction));
        }
    }
}
