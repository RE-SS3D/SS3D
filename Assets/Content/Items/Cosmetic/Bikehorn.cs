using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Items.Cosmetic
{
    [RequireComponent(typeof(AudioSource))]
    public class Bikehorn : Item, IInteractionTarget
    {
        private class HonkInteraction : IInteraction
        {
            public IClientInteraction CreateClient(InteractionEvent interactionEvent)
            {
                return null;
            }

            public string GetName(InteractionEvent interactionEvent)
            {
                return "Honk";
            }

            public bool CanInteract(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is Bikehorn horn)
                {
                    if (!InteractionHelpers.RangeCheck(interactionEvent))
                    {
                        return false;
                    }
                    return !horn.IsHonking();
                }
                if (interactionEvent.Source is Bikehorn horn1)
                {
                    return !horn1.IsHonking();
                }

                return false;
            }

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (interactionEvent.Target is Bikehorn horn)
                {
                    horn.Honk();
                }
                if (interactionEvent.Source is Bikehorn horn1)
                {
                    horn1.Honk();
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
        
        [SerializeField] private AudioClip honkSound = null;
        private AudioSource audioSource;

        public void Start()
        {
            audioSource = GetComponent<AudioSource>();
            GenerateNewIcon(); 
        }

        private bool IsHonking()
        {
            return audioSource.isPlaying;
        }

        [Server]
        private void Honk()
        {
            audioSource.PlayOneShot(honkSound);
            RpcPlayHonk();
        }

        [ClientRpc]
        private void RpcPlayHonk()
        {
            audioSource.PlayOneShot(honkSound);
        }

        public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
        {
            List<IInteraction> interactions = base.GenerateInteractions(targets).ToList();
            interactions.Insert(0, new HonkInteraction());
            return interactions.ToArray();
        }

        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]{new HonkInteraction()};
        }
    }
}