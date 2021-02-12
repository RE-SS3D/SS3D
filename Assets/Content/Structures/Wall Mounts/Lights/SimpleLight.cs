using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLight : InteractionTargetNetworkBehaviour
{
    public Sprite turnOnIcon;
    public Material onMaterial;
    public Material offMaterial;
    public MeshRenderer bulbRenderer;

    private Light[] lights;
    private bool on = true;

    void Start()
    {
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

        foreach (Light light in lights)
        {
            light.enabled = on;
        }

        bulbRenderer.material = on ? onMaterial : offMaterial;

        RpcToggle(on);
    }


    [ClientRpc]
    private void RpcToggle(bool on)
    {
        foreach (Light light in lights)
        {
            light.enabled = on;
        }

        bulbRenderer.material = on ? onMaterial : offMaterial;
    }
}
