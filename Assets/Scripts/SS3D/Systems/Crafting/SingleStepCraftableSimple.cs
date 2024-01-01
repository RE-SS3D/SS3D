using FishNet;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script for things that upon applying the recipe will just spawn at the interaction point.
/// </summary>
public class SingleStepCraftableSimple : SingleStepCraftable
{
    public override void Craft(GameObject instance, IInteraction interaction, InteractionEvent interactionEvent)
    {
        instance.transform.position = interactionEvent.Point;
        InstanceFinder.ServerManager.Spawn(instance);
        instance.SetActive(true);
    }

    public override void Modify(IInteraction interaction, InteractionEvent interactionEvent){ }
}
