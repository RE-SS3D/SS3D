﻿using SS3D.Content.Furniture.Storage;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using UnityEngine;

namespace SS3D.Content.Furniture.Machines.Atmospherics.DisposalBin
{
    // This handles the disposal bin object
    public class DisposalBin : InteractionTargetNetworkBehaviour
    {
        public Animator animator;

	// the container inside the bin
        public Container container;

	// is the bin dumping trash right now?
        public bool busy = false;
        
        public AudioSource audioSource;

        public float range;

	// Interaction that handles the disposal of items
	// This should be reworked once we have disposals pipes going
	// Currently it only deletes the items
        private class DisposeInteraction : IInteraction
        {
            public Sprite icon;

            public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
            {
                throw new System.NotImplementedException();
            }

            public bool CanInteract(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is DisposalBin disposal)
                {
                    if (!InteractionExtensions.RangeCheck(interactionEvent))
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }

            public IClientInteraction CreateClient(InteractionEvent interactionEvent)
            {
                return null;
            }

            public Sprite GetIcon(InteractionEvent interactionEvent)
            {
                return icon;
            }

            public string GetName(InteractionEvent interactionEvent)
            {
                return "Dispose Content";
            }

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (interactionEvent.Target is DisposalBin disposal)
                {
		    // Calls the dispose method on the bin
                    disposal.DisposeContents();
                }
                return false;
            }

            public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
            {
                throw new System.NotImplementedException();
            }
        }
    
        public void DisposeContents()
        {
            if (!animator.IsInTransition(0))
            {
                animator.SetTrigger("Dispose");
                audioSource.Play();
                
                //busy = true;
                // TODO: busy = false and Purge() or Dump() ? are called on the animation clip event
		// ^ probably done already, check the animation, delete those lines if its already done
            }
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>();
            
            StoreInteraction storeInteraction = new StoreInteraction();

	    // Sets the interaction range
            ViewContainerInteraction view = new ViewContainerInteraction { MaxDistance = range };
            DisposeInteraction disposeInteraction = new DisposeInteraction();

	    // if we arent purging something already, we create the interactions
            if (!busy)
            {
                interactions.Insert(0, storeInteraction);
                interactions.Insert(1, view);
                interactions.Insert(2, disposeInteraction);
            }

            return interactions.ToArray();
        }

    }
}
