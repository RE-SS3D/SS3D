using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Items.Functional.Weapons
{
    // Handles the pepper spray item, currently just uses a particle system
    public class PepperSpray: Item
    {
        private class SprayInteraction : IInteraction
        {
            public Sprite icon;

            public IClientInteraction CreateClient(InteractionEvent interactionEvent)
            {
                return null;
            }

            public string GetName(InteractionEvent interactionEvent)
            {
                return "Spray";
            }

            public Sprite GetIcon(InteractionEvent interactionEvent)
            {
                return icon;
            }

            public bool CanInteract(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is PepperSpray spray)
                {
                    return InteractionExtensions.RangeCheck(interactionEvent) && spray.CanSpray();
                }
                return false;
            }

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (interactionEvent.Target is PepperSpray spray)
                {
                    spray.Spray();
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

        [SerializeField] private AudioClip spraySound = null;
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private float cooldownInSeconds;

        public Sprite useIcon;
        private AudioSource audioSource;
        private float lastSprayTime;
        private bool justSprayed;

        public override void Start()
        {
            base.Start();
            audioSource = GetComponent<AudioSource>();
            GenerateNewIcon();
        }

        public override void Update()
        {
            base.Update();
            if (justSprayed)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                justSprayed = false;
            }
        }

        private bool CanSpray()
        {
            return Time.time - lastSprayTime >= cooldownInSeconds;
        }

        [Server]
        private void Spray()
        {
            lastSprayTime = Time.time;
            particles.Play();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(spraySound);
            }
            justSprayed = true;
            RpcSpray();
        }

        [ClientRpc]
        private void RpcSpray()
        {
            lastSprayTime = Time.time;
            particles.Play();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(spraySound);
            }
            justSprayed = true;
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            list.Add(new SprayInteraction { icon = useIcon });
            return list.ToArray();
        }
    }
}
