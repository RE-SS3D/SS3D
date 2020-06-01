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
    public class Bikehorn : Item
    {
        private class HonkInteraction : IInteraction
        {

            public Sprite icon;

            public IClientInteraction CreateClient(InteractionEvent interactionEvent)
            {
                return null;
            }

            public string GetName(InteractionEvent interactionEvent)
            {
                return "Honk";
            }

            public Sprite GetIcon(InteractionEvent interactionEvent)
            {
                return icon;
            }

            public bool CanInteract(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is Bikehorn horn)
                {
                    if (!InteractionExtensions.RangeCheck(interactionEvent))
                    {
                        return false;
                    }
                    return !horn.IsHonking();
                }

                return false;
            }

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (interactionEvent.Target is Bikehorn horn)
                {
                    horn.Honk();
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

        public Sprite useIcon;

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
        
        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractions(interactionEvent).ToList();
            list.Add(new HonkInteraction{ icon = useIcon });
            return list.ToArray();
        }
    }
}