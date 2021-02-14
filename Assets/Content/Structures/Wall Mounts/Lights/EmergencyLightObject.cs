using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmergencyLightObject : InteractionTargetNetworkBehaviour
{
    private static readonly int OnHash = Animator.StringToHash("Active");

    public Sprite turnOnIcon;
    private Animator animator;

    private Light[] lights;
    private bool on;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        lights = GetComponentsInChildren<Light>();
    }


    public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
    {
        var interaction = new SimpleInteraction
        {
            Name = on ? "Turn off" : "Turn on",
            icon = turnOnIcon,
            CanInteractCallback = InteractionExtensions.RangeCheck,
            Interact = Toggle,
        };
        return new IInteraction[] { interaction };
    }

    
    private void Toggle(InteractionEvent interactionEvent, InteractionReference reference)
    {
        on = !on;

        animator.SetBool(OnHash, on);
        foreach (Light light in lights)
        {
            light.enabled = on;
        }

        RpcToggle(on);
    }

    [ClientRpc]
    private void RpcToggle(bool on)
    {
        animator.SetBool(OnHash, on);
        foreach (Light light in lights)
        {
            light.enabled = on;
        }
    }
}
