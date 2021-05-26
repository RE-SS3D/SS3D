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

// Handles the microwave object
[RequireComponent(typeof(AudioSource))]
public class Microwave : InteractionTargetNetworkBehaviour
{
    // Duration of the cycle
    public float MicrowaveDuration = 5;
    // Prefab for the ashes, when an item is burned
    // TODO: implement the assetdata stuffs
    public GameObject DestroyedItemPrefab;
    // Place for the container to place items that is placed in the object
    public AttachedContainer AttachedContainer;

    private AudioSource audioSource;
    // Sound that plays when its turned on
    public AudioClip onSound;
    // Sound that plays when it ends a cycle
    public AudioClip finishSound;

    //used to enable & disable microwave lights
    private Material emissionMaterial;
    private Light light;

    // is it being used rn
    // should probably be renamed to busy
    // we might have isOn for electricity stuff
    private bool isOn;
    // actual container
    private OpenableContainer openableContainer;

    private void Start()
    {
        Assert.IsNotNull(AttachedContainer);

        openableContainer = GetComponent<OpenableContainer>();
        audioSource = GetComponent<AudioSource>();

        emissionMaterial = GetComponent<Renderer>().materials[1];
        emissionMaterial.DisableKeyword("_EMISSION");
        light = GetComponentInChildren<Light>();
        light.enabled = false;
    }

    public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[] {new SimpleInteraction
        {
            // TODO: Should be a custom interaction
            Name = "Turn on", CanInteractCallback = CanTurnOn, Interact = TurnOn
        }};
    }
    
    private bool CanTurnOn(InteractionEvent interactionEvent)
    {
        if (!InteractionExtensions.RangeCheck(interactionEvent))
        {
            return false;
        }

        // Can't be turned on if the door is open, we might add a hacking thing to bypass this later
        if (openableContainer != null && openableContainer.IsOpen())
        {
            return false;
        }

        return !isOn;
    }

    private void TurnOn(InteractionEvent interactionEvent, InteractionReference reference)
    {
        SetActivated(true);
        RunMicrowave();
        // Great naming
        StartCoroutine(BlastShit());
    }

    // Sets the state to busy
    private void SetActivated(bool activated)
    {
        isOn = activated;
        if (openableContainer != null)
        {
            openableContainer.enabled = !activated;
        }
    }

    // Start a cycle
    private IEnumerator BlastShit()
    {
        // waits until the cycle has finished
        yield return new WaitForSeconds(MicrowaveDuration);
        StopMicrowave();
        SetActivated(false);

        // Process the contents
        CookItems();
    }

    private void CookItems()
    {
        var items = AttachedContainer.Container.Items.ToArray();

        // tries to get a microweavable in each item that is in the container
        foreach (Item item in items)
        {
            Microwaveable microwaveable = item.GetComponent<Microwaveable>();
            if (microwaveable != null)
            {
                // if the microwaveable has a result we produce it
                ItemHelpers.ReplaceItem(item, ItemHelpers.CreateItem(microwaveable.ResultingObject));
            }
            else
            {
                // if there's no recipe we throw trash
                ItemHelpers.ReplaceItem(item, ItemHelpers.CreateItem(DestroyedItemPrefab));
            }
        }
    }

    [Server]
    private void StopMicrowave()
    {
        emissionMaterial.DisableKeyword("_EMISSION");
        light.enabled = false;

        audioSource.Stop();
        audioSource.PlayOneShot(finishSound);
        RpcStopMicrowave();
    }

    [ClientRpc]
    private void RpcStopMicrowave()
    {
        emissionMaterial.DisableKeyword("_EMISSION");
        light.enabled = false;

        audioSource.Stop();
        audioSource.PlayOneShot(finishSound);
    }

    [Server]
    private void RunMicrowave()
    {
        emissionMaterial.EnableKeyword("_EMISSION");
        light.enabled = true;

        audioSource.Stop();
        audioSource.clip = onSound;
        audioSource.Play();
        RpcRunMicrowave();
    }

    [ClientRpc]
    private void RpcRunMicrowave()
    {
        emissionMaterial.EnableKeyword("_EMISSION");
        light.enabled = true;

        audioSource.Stop();
        audioSource.clip = onSound;
        audioSource.Play();
    }
}
