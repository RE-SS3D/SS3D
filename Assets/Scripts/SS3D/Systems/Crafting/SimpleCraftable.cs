using FishNet;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCraftable : NetworkActor, ICraftable
{
    public void Craft(IInteraction interaction, InteractionEvent interactionEvent)
    {
        GameObject instance = Instantiate(gameObject);
        instance.transform.position = interactionEvent.Point;
        InstanceFinder.ServerManager.Spawn(instance);
        instance.SetActive(true);
    }
}
