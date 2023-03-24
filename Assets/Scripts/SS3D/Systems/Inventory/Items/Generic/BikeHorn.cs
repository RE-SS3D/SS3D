using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    public class BikeHorn : ItemActor
    {
        [Header("Bike horn settings")]
        [SerializeField] private AudioClip _honkSound;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Animator _animator;

        [SerializeField] private Sprite _honkIcon;

        private static readonly int HonkAnimation = Animator.StringToHash("Honk");

        protected override void OnStart()
        {
            base.OnStart();

            _audioSource.clip = _honkSound;
        }

        [ServerRpc(RequireOwnership = false)]
        public void Honk()
        {
            _audioSource.Play();
            _animator.SetTrigger(HonkAnimation);

            RpcPlayHonk();
        }

        [ObserversRpc]
        private void RpcPlayHonk()
        {
            if (IsServer) { return; }

            _audioSource.Play();
            _animator.SetTrigger(HonkAnimation);
        }

        public bool IsHonking()
        {
            bool audioSourceExists = _audioSource != null;
            bool isPlaying = _audioSource.isPlaying;

            // If our audio source exists, and it's rigged up with our honk sound, and it's our child,
            // check if it's playing. Otherwise, it's honkin' time.
            return audioSourceExists && isPlaying;
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();
            HonkInteraction honk = new() { Icon = _honkIcon };

            interactions.Add(honk);

            return interactions.ToArray();
        }
    }
}