using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;

public class Nuke : InteractionTargetNetworkBehaviour
{
    public int explosionDelay = 10;
    public AudioClip beepSound;
    public AudioSource audioSource;

    public bool canDefuse;
    
    public Sprite interactionIcon;

    // Activates the nuke explosion sequence
    public void Boom()
    {
        
    }

    [ClientRpc]
    private void RpcBoom()
    {
        
    }
    public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
    {
        IInteraction[] interactions = new[]
        {
            // Temporary interaction icon
            new ActivateNukeInteraction() { icon = interactionIcon }
        };

        return interactions;
    }

    public class ActivateNukeInteraction : SimpleInteraction
    {
        public string GetName(InteractionEvent interactionEvent)
        {
            return "Activate nuke";
        }
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is Nuke nuke)
            {
                // Activates the nuke explosion sequence
                nuke.Boom();
                return true;
            }

            return false;
        }
    }
}
