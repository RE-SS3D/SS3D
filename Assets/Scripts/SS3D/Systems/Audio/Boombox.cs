using FishNet.Object.Synchronizing;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core;
using System.Electricity;
using UnityEngine.Serialization;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Script for jukeboxes and boomboxes, allowing switching between different sounds and toggling it on and off.
    /// </summary>
    public class Boombox : InteractionTargetNetworkBehaviour, IToggleable
    {
        [SerializeField]
        private MachinePowerConsumer _powerConsumer;
        
        [SerializeField]
        private List<AudioClip> _songs;

        [SyncVar]
        public bool AudioOn;

        [SyncVar]
        public int CurrentMusic;

        // TODO: Update this file with boombox icons from asset data.
        public Sprite InteractionIcon;
        public Sprite InteractionIconOn;

        public bool GetState()
        {
            return AudioOn;
        }
        
        protected override void OnEnabled()
        {
            base.OnEnabled();
            
            _powerConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
        }

        public void Toggle()
        {
            if (_powerConsumer.PowerStatus != PowerStatus.Powered)
            {
                return;
            }
            
            AudioOn = !AudioOn;
            _powerConsumer.isIdle = !AudioOn;
            
            if (AudioOn)
            {
                SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, _songs[CurrentMusic], GameObject.transform.position, NetworkObject,
                    false, 0.7f, 1, 1, 5);
            }
            else
            {
                SubSystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            }
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            UpdateMusic(newStatus);
        }

        private void UpdateMusic(PowerStatus powerStatus)
        {
            if (AudioOn && powerStatus != PowerStatus.Powered)
            {
                AudioOn = false;
                SubSystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            }
        }

        public void ChangeCurrentMusic()
        {
            if (!AudioOn)
            {
                return;
            }
            
            SubSystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            SubSystems.Get<AudioSubSystem>().SetTimeAudioSource(NetworkObject, 0f);
            CurrentMusic = (CurrentMusic + 1) % (_songs.Count);
            SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, _songs[CurrentMusic], GameObject.transform.position, NetworkObject,
                false, 0.7f, 1, 1, 5);
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
