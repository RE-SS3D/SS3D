using FishNet.Object.Synchronizing;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Data.Enums;
using SS3D.Core;

namespace SS3D.Systems.Audio
{
    public class Boombox : InteractionTargetNetworkBehaviour, IToggleable
    {
        // musics? wonder if there was another word for it, sons, sonds, sonids uhmmm
        [SerializeField]
        private AudiosIds[] soundsIds;

        // is it playing music

        public bool radioOn;
        public int currentMusic;

        // I hate my life
        public Sprite interactionIcon;
        public Sprite interactionIconOn;


        public void Toggle()
        {
            radioOn = !radioOn;
            if (!radioOn)
            {

                Subsystems.Get<AudioSystem>().StopAudioSource(NetworkObject);
            }
            else
            {
                Subsystems.Get<AudioSystem>().PlayAudioSource(soundsIds[currentMusic], GameObject.transform.position, NetworkObject, 0.7f, 1, 1, 5);
            }
        }

        public void ChangeCurrentMusic()
        {
            Subsystems.Get<AudioSystem>().StopAudioSource(NetworkObject);
            Subsystems.Get<AudioSystem>().SetTimeAudioSource(NetworkObject, 0f);
            currentMusic = (currentMusic + 1) % (soundsIds.Length);
            Subsystems.Get<AudioSystem>().PlayAudioSource(soundsIds[currentMusic], GameObject.transform.position, NetworkObject, 0.7f, 1, 1, 5);
        }

        public bool GetState()
        {
            return radioOn;
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>(2)
            {
                new ChangeMusicInteraction()
            };
            ToggleInteraction toggleInteraction = new ToggleInteraction
            {
                IconOn = interactionIconOn,
                IconOff = interactionIconOn,
            };

            interactions.Insert(GetState() ? interactions.Count : interactions.Count - 1, toggleInteraction);
            return interactions.ToArray();
        }
    }
}
