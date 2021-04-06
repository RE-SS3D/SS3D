using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Furniture.Storage;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

[RequireComponent(typeof(AudioSource))]
public class Microwave : InteractionTargetNetworkBehaviour
{
    public float MicrowaveDuration = 5;
    public GameObject DestroyedItemPrefab;
    public AttachedContainer AttachedContainer;

    private AudioSource audioSource;
    public AudioClip onSound;
    public AudioClip finishSound;

    private Material emissionMat;

    private bool isOn;
    private StorageContainer storageContainer;

    private void Start()
    {
        Assert.IsNotNull(AttachedContainer);
        
        storageContainer = GetComponent<StorageContainer>();
        audioSource = GetComponent<AudioSource>();

        emissionMat = GetComponent<Renderer>().materials[1];
        emissionMat.DisableKeyword("_EMISSION");
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
        Running();
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
        FinishRun();
        SetActivated(false);
        CookItems();
    }

    private void CookItems()
    {
        var items = AttachedContainer.Container.Items.ToArray();
        foreach (Item item in items)
        {
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
    private void FinishRun()
    {
        emissionMat.DisableKeyword("_EMISSION");

        audioSource.Stop();
        audioSource.PlayOneShot(finishSound);
        RpcFinishRun();
    }

    [ClientRpc]
    private void RpcFinishRun()
    {
        emissionMat.DisableKeyword("_EMISSION");

        audioSource.Stop();
        audioSource.PlayOneShot(finishSound);
    }

    [Server]
    private void Running()
    {
        emissionMat.EnableKeyword("_EMISSION"); 

        audioSource.Stop();
        audioSource.clip = onSound;
        audioSource.Play();
        RpcRunning();
    }

    [ClientRpc]
    private void RpcRunning()
    {
        emissionMat.EnableKeyword("_EMISSION");

        audioSource.Stop();
        audioSource.clip = onSound;
        audioSource.Play();
    }
}
