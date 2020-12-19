using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Content.Furniture.Storage;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Container))]
[RequireComponent(typeof(AudioSource))]
public class Microwave : InteractionTargetNetworkBehaviour
{
    public float MicrowaveDuration = 5;
    public GameObject DestroyedItemPrefab;

    private AudioSource audioSource;
    public AudioClip onSound;
    public AudioClip finishSound;

    private bool isOn;
    private StorageContainer storageContainer;
    private Container container;

    private void Start()
    {
        storageContainer = GetComponent<StorageContainer>();
        container = GetComponent<Container>();
        audioSource = GetComponent<AudioSource>();
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
        PlayOnSnd();
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
        PlayFinishSnd();
        SetActivated(false);
        CookItems();
    }

    private void CookItems()
    {
        List<Item> items = container.GetItems();
        for (var i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null)
            {
                continue;
            }
            Microwaveable microwaveable = item.GetComponent<Microwaveable>();
            if (microwaveable != null)
            {

                ItemHelpers.ReplaceItem(item, ItemHelpers.CreateItem(microwaveable.ResultingObject));
            }
            else
            {
                ItemHelpers.ReplaceItem(item, ItemHelpers.CreateItem(DestroyedItemPrefab));
            }
        }
    }

    [Server]
    private void PlayFinishSnd()
    {
        audioSource.Stop();
        audioSource.PlayOneShot(finishSound);
        RpcPlayFinishSnd();
    }

    [ClientRpc]
    private void RpcPlayFinishSnd()
    {
        audioSource.Stop();
        audioSource.PlayOneShot(finishSound);
    }

    [Server]
    private void PlayOnSnd()
    {
        audioSource.Stop();
        audioSource.clip = onSound;
        audioSource.Play();
        RpcPlayOnSnd();
    }

    [ClientRpc]
    private void RpcPlayOnSnd()
    {
        audioSource.Stop();
        audioSource.clip = onSound;
        audioSource.Play();
    }
}
