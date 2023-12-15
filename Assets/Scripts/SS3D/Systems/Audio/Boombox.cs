using FishNet.Object.Synchronizing;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core;
using UnityEngine.Serialization;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Script for jukeboxes and boomboxes, allowing switching between different sounds and toggling it on and off.
    /// </summary>
    public class Boombox : InteractionTargetNetworkBehaviour, IToggleable
    {
        [SerializeField]
        private List<AudioClip> _songs;

        // is it playing music
        [SyncVar]
        public bool RadioOn;

        [SyncVar]
        public int CurrentMusic;

        // TODO: Update this file with boombox icons from asset data.
        public Sprite InteractionIcon;
        public Sprite InteractionIconOn;

        public void Toggle()
        {
            RadioOn = !RadioOn;
            if (!RadioOn)
            {

                Subsystems.Get<AudioSystem>().StopAudioSource(NetworkObject);
            }
            else
            {
                Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.Music, _songs[CurrentMusic], GameObject.transform.position, NetworkObject, 0.7f, 1, 1, 5);
            }
        }

        public void ChangeCurrentMusic()
        {
            Subsystems.Get<AudioSystem>().StopAudioSource(NetworkObject);
            Subsystems.Get<AudioSystem>().SetTimeAudioSource(NetworkObject, 0f);
            CurrentMusic = (CurrentMusic + 1) % (_songs.Count);
            Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.Music, _songs[CurrentMusic], GameObject.transform.position, NetworkObject, 0.7f, 1, 1, 5);
        }

        public bool GetState()
        {
            return RadioOn;
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>(2)
            {
                new ChangeMusicInteraction()
            };
            ToggleInteraction toggleInteraction = new ToggleInteraction
            {
                IconOn = InteractionIconOn,
                IconOff = InteractionIconOn,
            };

            interactions.Insert(GetState() ? interactions.Count : interactions.Count - 1, toggleInteraction);
            return interactions.ToArray();
        }
    }
}
