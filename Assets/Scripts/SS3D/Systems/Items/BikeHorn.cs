using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Storage.Items;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Items
{
    public class BikeHorn : Item
    {
        [Header("Bike horn settings")]
        [SerializeField] private AudioClip _honkSound;
        [SerializeField] private AudioSource _audioSource;

        [SerializeField] private Sprite _honkIcon;

        protected override void OnStart()
        {
            base.OnStart();

            _audioSource.clip = _honkSound;
        }

        [ServerRpc(RequireOwnership = false)]
        public void Honk()
        {
            _audioSource.Play();

            RpcPlayHonk();
        }

        [ObserversRpc]
        private void RpcPlayHonk()
        {
            if (IsServer) { return; }

            _audioSource.Play();
        }

        public bool IsHonking()
        {
            // If our audio source exists, and it's rigged up with our honk sound, and it's our child, check if it's playing. Otherwise, it's honkin' time.
            if (_audioSource != null)
            {
                return _audioSource.isPlaying;  
            }

            return false;
        }
        
        public override IInteraction[] GetTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.GetTargetInteractions(interactionEvent).ToList();
            
            HonkInteraction honk = new() { Icon = _honkIcon };

            interactions.Add(honk);

            return interactions.ToArray();
        }
    }
}