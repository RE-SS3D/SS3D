using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    public class BikeHorn : Item
    {
        [Header("Bike horn settings")]
        [SerializeField] private Animator _animator;

        [SerializeField] private Sprite _honkIcon;

        private static readonly int HonkAnimation = Animator.StringToHash("Honk");
        
        private AssetHandle<AudioClip> _honkClipHandle;

        public void Honk()
        {
            _animator.SetTrigger(HonkAnimation);
            SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Sfx, Sounds.BikeHorn, GameObject.transform.position, NetworkObject, false, 0.7f, 1, 1, 5);
        }

        public bool IsHonking()
        {
            // If our audio source exists, and it's rigged up with our honk sound, and it's our child,
            // check if it's playing. Otherwise, it's honkin' time.
            return TryGetComponent(out AudioSource audioSource) && audioSource.isPlaying;
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();
            HonkInteraction honk = new() { Icon = _honkIcon };

            interactions.Add(honk);

            return interactions.ToArray();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            AcquireAudioClips();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            ReleaseAudioClips();
        }

        private async void AcquireAudioClips()
        {
            _honkClipHandle = await new AssetRequest<AudioClip>(Sounds.BikeHorn).ExecuteAsync();

            if (!_honkClipHandle)
            {
                ReleaseHandle(ref _honkClipHandle);
            }
        }
        
        private void ReleaseAudioClips()
        {
            ReleaseHandle(ref _honkClipHandle);
        }

        private void ReleaseHandle<T>([CanBeNull] ref AssetHandle<T> assetHandle)
            where T : Object
        {
            assetHandle?.Dispose();
            assetHandle = null;
        }
    }
}