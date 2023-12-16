using SS3D.Core.Behaviours;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCraftable : NetworkActor, ICraftable
{
    public void Craft(InteractionEvent interaction)
    {
        GameObject instance = Instantiate(gameObject);
        instance.transform.position = interaction.Point;
        Spawn(instance);
        instance.SetActive(true);
    }
}
