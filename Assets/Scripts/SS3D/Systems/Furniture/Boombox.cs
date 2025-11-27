using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Script for jukeboxes and boomboxes, allowing switching between different sounds and toggling it on and off.
    /// </summary>
    public class Boombox : NetworkActor, IToggleable, IInteractionTarget
    {
        [SerializeField]
        private MachinePowerConsumer _powerConsumer;

        [SerializeField]
        private List<AudioClip> _songs;

        [SyncVar]
        private bool _audioOn;

        [SyncVar]
        private int _currentMusic;

        [FormerlySerializedAs("InteractionIcon")]
        [SerializeField]
        private Sprite _interactionIcon;

        [FormerlySerializedAs("InteractionIconOn")]
        [SerializeField]
        private Sprite _interactionIconOn;

        public Sprite InteractionIcon => _interactionIcon;

        public bool GetState() => _audioOn;

        public void Toggle()
        {
            if (_powerConsumer.PowerStatus != PowerStatus.Powered)
            {
                return;
            }

            _audioOn = !_audioOn;
            _powerConsumer.IsIdle = !_audioOn;

            if (_audioOn)
            {
                Subsystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, _songs[_currentMusic], GameObject.transform.position, NetworkObject, false, 0.7f, 1, 1, 5);
            }
            else
            {
                Subsystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            }
        }

        public void ChangeCurrentMusic()
        {
            if (!_audioOn)
            {
                return;
            }

            Subsystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            Subsystems.Get<AudioSubSystem>().SetTimeAudioSource(NetworkObject, 0f);
            _currentMusic = (_currentMusic + 1) % _songs.Count;
            Subsystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, _songs[_currentMusic], GameObject.transform.position, NetworkObject, false, 0.7f, 1, 1, 5);
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>(2)
            {
                new ChangeMusicInteraction(),
            };

            ToggleInteraction toggleInteraction = new ToggleInteraction
            {
                IconOn = _interactionIconOn,
                IconOff = _interactionIconOn,
            };

            interactions.Insert(GetState() ? interactions.Count : interactions.Count - 1, toggleInteraction);
            return interactions.ToArray();
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        protected override void OnEnabled()
        {
            base.OnEnabled();
            _powerConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            UpdateMusic(newStatus);
        }

        private void UpdateMusic(PowerStatus powerStatus)
        {
            if (_audioOn && powerStatus != PowerStatus.Powered)
            {
                _audioOn = false;
                Subsystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            }
        }
    }
}
