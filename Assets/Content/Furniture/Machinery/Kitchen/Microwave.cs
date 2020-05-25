using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Content.Furniture.Storage;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

[RequireComponent(typeof(Container))]
public class Microwave : InteractionTargetNetworkBehaviour
{
    public float MicrowaveDuration = 5;
    private bool isOn;
    private StorageContainer storageContainer;
    private Container container;

    private void Start()
    {
        storageContainer = GetComponent<StorageContainer>();
        container = GetComponent<Container>();
    }

    public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[] {new SimpleInteraction
        {
            Name = "Turn on", CanInteractCallback = CanTurnOn, Interact = TurnOn
        }};
    }

    private bool CanTurnOn(InteractionEvent interactionEvent)
    {
        if (!InteractionExtensions.RangeCheck(interactionEvent))
        {
            return false;
        }

        if (storageContainer != null && storageContainer.IsOpen())
        {
            return false;
        }

        return !isOn;
    }

    private void TurnOn(InteractionEvent interactionEvent, InteractionReference reference)
    {
        SetActivated(true);
        StartCoroutine(BlastShit());
    }

    private void SetActivated(bool activated)
    {
        isOn = activated;
        if (storageContainer != null)
        {
            storageContainer.enabled = !activated;
        }
    }

    private IEnumerator BlastShit()
    {
        yield return new WaitForSeconds(MicrowaveDuration);
        SetActivated(false);
        CookItems();
    }

    private void CookItems()
    {
        List<Item> items = container.GetItems();
        for (var i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            Microwaveable microwaveable = item.GetComponent<Microwaveable>();
            if (microwaveable != null)
            {
                
                ItemHelpers.ReplaceItem(item, ItemHelpers.CreateItem(microwaveable.ResultingObject));
            }
        }
    }
}
