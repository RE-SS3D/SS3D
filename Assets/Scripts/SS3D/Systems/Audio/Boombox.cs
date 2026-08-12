using FishNet.Object.Synchronizing;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using System.Electricity;

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
        private List<ObjectAssetReference> _songReferences;

        [SyncVar]
        public bool AudioOn;

        [SyncVar]
        public int CurrentMusic;

        // TODO: Update this file with boombox icons from asset data.
        public Sprite InteractionIcon;
        public Sprite InteractionIconOn;
        
        private AssetHandle<AudioClip>[] _songHandles;

        public bool GetState()
        {
            return AudioOn;
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
                SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, _songReferences[CurrentMusic].Id, GameObject.transform.position, NetworkObject,
                    false, 0.7f, 1, 1, 5);
            }
            else
            {
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
            CurrentMusic = (CurrentMusic + 1) % _songReferences.Count;
            SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, _songReferences[CurrentMusic].Id, GameObject.transform.position, NetworkObject,
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

        protected override void OnAwake()
        {
            base.OnAwake();
            AcquireAudioClips();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            _powerConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            ReleaseAudioClips();
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

        private async void AcquireAudioClips()
        {
            _songHandles = new AssetHandle<AudioClip>[_songReferences.Count];

            for (int i = 0; i < _songReferences.Count; i++)
            {
                _songHandles[i] = await new AssetRequest<AudioClip>(_songReferences[i]).LoadAsync();

                if (!_songHandles[i])
                {
                    AssetHandle.Release(ref _songHandles[i]);
                }
            }
        }

        private void ReleaseAudioClips()
        {
            for (int i = 0; i < _songHandles.Length; i++)
            {
                AssetHandle.Release(ref _songHandles[i]);
            }
        }
    }
}
