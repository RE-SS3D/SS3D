using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using UnityEngine;

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


        public void Honk()
        {
            _animator.SetTrigger(HonkAnimation);
            Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.sfx, AudiosIds.BikeHorn, GameObject.transform.position, NetworkObject, 0.7f, 1, 1, 5);
        }

        public bool IsHonking()
        {
            if(!TryGetComponent<AudioSource>(out var audioSource)) return false;

            // If our audio source exists, and it's rigged up with our honk sound, and it's our child,
            // check if it's playing. Otherwise, it's honkin' time.
            return audioSource.isPlaying;
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