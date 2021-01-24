using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Items.Cosmetic
{
    [RequireComponent(typeof(AudioSource))]
    public class ServiceBell : Item, IInteractionTarget
    {
        public Sprite interactionIcon;

        private class BellInteraction : IInteraction
        {
            public IClientInteraction CreateClient(InteractionEvent interactionEvent)
            {
                return null;
            }

            public string GetName(InteractionEvent interactionEvent)
            {
                return "Bell";
            }

            public Sprite GetIcon(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is ServiceBell bell)
                    return bell.interactionIcon;
                if (interactionEvent.Source is ServiceBell bell1)
                    return bell1.interactionIcon;
                return null;
            }

            public bool CanInteract(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is ServiceBell bell)
                {
                    if (!InteractionExtensions.RangeCheck(interactionEvent))
                    {
                        return false;
                    }
                    return true;
                }

                return false;
            }

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (interactionEvent.Target is ServiceBell bell)
                {
                    bell.Bell();
                }
                return false;
            }

            public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
            {
                throw new System.NotImplementedException();
            }

            public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
            {
                throw new System.NotImplementedException();
            }
        }
        
        [SerializeField] private AudioClip bellSound = null;
        private AudioSource audioSource;

        public override void Start()
        {
            base.Start();
            audioSource = GetComponent<AudioSource>();
            GenerateNewIcon(); 
        }

        [Server]
        private void Bell()
        {
            audioSource.PlayOneShot(bellSound);
            RpcPlayBell();
        }

        [ClientRpc]
        private void RpcPlayBell()
        {
            audioSource.PlayOneShot(bellSound);
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.GenerateInteractions(interactionEvent).ToList();
            interactions.Insert(0, new BellInteraction());
            return interactions.ToArray();
        }
    }
}