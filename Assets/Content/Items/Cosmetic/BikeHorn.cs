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
    public class BikeHorn : Item
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
                if (interactionEvent.Target is BikeHorn horn)
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
                if (interactionEvent.Target is BikeHorn horn)
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

        public override void Start()
        {
            base.Start();
            GenerateNewIcon();
        }

        private bool IsHonking()
        {
            //If our audio source exists, and it's rigged up with our honk sound, and it's our child, check if it's playing. Otherwise, it's honkin' time.
            if (audioSource != null && (audioSource.clip == honkSound) && (gameObject.transform == audioSource.transform.parent))
            {
                return audioSource.isPlaying;  
            }
            else
            {
                return false;
            }
        }

        [Server]
        private void Honk()
        {
            //Grab a specific instance of an audio source from our audio manager. This is used by the honker to determine if we are honkin'.
            audioSource = AudioManager.Instance.FindAvailableAudioSource();
            EmitSound(audioSource);
            RpcPlayHonk();
        }

        [ClientRpc]
        private void RpcPlayHonk()
        {
            EmitSound(audioSource);
        }
        
        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            list.Add(new HonkInteraction{ icon = useIcon });
            return list.ToArray();
        }

        private void EmitSound(AudioSource specificSource)
        {
            GameObject thisObject = gameObject;
            AudioManager.Instance.PlayAudioSourceSpecific(specificSource, honkSound, thisObject.transform.position, thisObject, 0.7f, Random.Range(0.9f, 1.1f));
        }
        
    }
}