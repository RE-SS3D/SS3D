using SS3D.Content.Furniture.Storage;
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
    public class DisposalBin : InteractionTargetNetworkBehaviour
    {
        public Animator animator;
        public Container container;
        public bool busy = false;
        
        public AudioSource audioSource;

        public float range;

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
                // TODO: busy = false and Purge() are called on the animation clip event
            }
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>();
            
            StoreInteraction storeInteraction = new StoreInteraction();
            ViewContainerInteraction view = new ViewContainerInteraction { MaxDistance = range };
            DisposeInteraction disposeInteraction = new DisposeInteraction();

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
