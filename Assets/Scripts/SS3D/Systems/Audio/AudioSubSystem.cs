using Coimbra;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Audio system handle creating and destroying audio sources, whether for SFX or music. It handle placing them at the right place, playing and stopping them.
    /// In the vast majority of cases you should use this to play sounds.
    /// </summary>
    public class AudioSubSystem : Core.Behaviours.NetworkSubSystem
    {
        private struct AudioSourcesList
        {
            public int MaxAudioSources;
            public int MinAudioSources;
            public AudioType AudioType;
            public List<AudioSource> List;
            public GameObject Prefab;
            public GameObject AudioSystem;

            public AudioSourcesList(int maxSources, int minSources, AudioType type, GameObject audioSourcePrefab, GameObject audioSystemGameObject)
            {
                AudioType = type;
                List = new List<AudioSource>();
                MinAudioSources = minSources;
                MaxAudioSources = maxSources;
                Prefab = audioSourcePrefab;
                AudioSystem = audioSystemGameObject;

                for (int i = 0; i < minSources; i++)
                {
                    CreateNewAudioSource();
                }
            }

            /// <summary>
            /// Creates a new audio source and adds it to the list.
            /// </summary>
            public void CreateNewAudioSource()
            {
                AudioSource newAudioSource = Instantiate(Prefab, AudioSystem.transform.position, Quaternion.identity).GetComponent<AudioSource>();
                List.Add(newAudioSource);
                newAudioSource.transform.parent = AudioSystem.transform;
            }

            /// <summary>
            /// Destroys all audio sources that aren't currently playing and removes them from the list.
            /// </summary>
            public void PurgeUnusedAudioSources()
            {
                foreach (AudioSource source in List)
                {
                    // Check that the audio source is idle, and we have more than our minimum number.
                    if (source && (source.isPlaying || List.Count <= MinAudioSources))
                    {
                        continue;
                    }

                    List.Remove(source);
                    if (source)
                    {
                        source.gameObject.Dispose(true);
                    }
                }
            }
        }

        [FormerlySerializedAs("SfxAudioSourcePrefab")]
        [Tooltip("The SFX audio source to be spawned.")]
        [SerializeField]
        private GameObject _sfxAudioSourcePrefab;

        [FormerlySerializedAs("MusicAudioSourcePrefab")]
        [Tooltip("The music audio source to be spawned.")]
        [SerializeField]
        private GameObject _musicAudioSourcePrefab;

        [FormerlySerializedAs("MinSfxAudioSources")]
        [Tooltip("The number of SFX audio sources to start out with.")]
        [SerializeField]
        private int _minSfxAudioSources = 10;

        [FormerlySerializedAs("MaxSfxAudioSources")]
        [Tooltip("Any more than this will be considered too many SFX audio sources.")]
        [SerializeField]
        private int _maxSfxAudioSources = 30;

        [FormerlySerializedAs("MinMusicAudioSources")]
        [Tooltip("The number of music audio sources to start out with.")]
        [SerializeField]
        private int _minMusicAudioSources = 3;

        [FormerlySerializedAs("MaxMusicAudioSources")]
        [Tooltip("Any more than this will be considered too many SFX audio sources.")]
        [SerializeField]
        private int _maxMusicAudioSources = 10;

        [Tooltip("How often (in minutes) do we purge unused audio sources?")]
        [SerializeField]
        private int _purgeFrequency = 2;

        private List<AudioSourcesList> _audioSourcesLists;

        /// <summary>
        /// Grabs a free audio source and parents it to a specific object before playing it.
        /// </summary>
        [Server]
        public void PlayAudioSource(AudioType audioType, AudioClip audioClipId, NetworkObject parent)
        {
            RpcPlayAudioSource(audioType, audioClipId.name, parent.transform.position, parent);
        }

        /// <summary>
        /// Plays a sound clip at a position, parent, with specific volume, pitch, and ranges.
        /// Volume, pitch, and ranges are optional.
        /// </summary>
        [Server]
        public void PlayAudioSource(
            AudioType audioType,
            AudioClip audioClipId,
            Vector3 position,
            NetworkObject parent,
            bool isLooping = false,
            float volume = 0.7f,
            float pitch = 1f,
            float minRange = 1f,
            float maxRange = 500f)
        {
            RpcPlayAudioSource(audioType, audioClipId.name, position, parent, isLooping, volume, pitch, minRange, maxRange);
        }

        [Server]
        public void StopAudioSource(NetworkObject parent)
        {
            RpcStopAudioSource(parent);
        }

        [Server]
        public void SetTimeAudioSource(NetworkObject parent, float time)
        {
            RPCSetTimeAudioSource(parent, time);
        }

        [ObserversRpc]
        public void RPCSetTimeAudioSource(NetworkObject parent, float time)
        {
            parent.GetComponentInChildren<AudioSource>().time = time;
        }

        [ObserversRpc]
        public void RpcPlayAudioSource(
            AudioType type,
            string audioClip,
            Vector3 position,
            NetworkObject parent,
            bool isLooping = false,
            float volume = 0.7f,
            float pitch = 1f,
            float minRange = 1f,
            float maxRange = 500f)
        {
            AudioSource audioSource = FindAvailableAudioSource(type);

            audioSource.gameObject.transform.position = position;
            audioSource.clip = Assets.Get<AudioClip>(AssetDatabases.Sounds, audioClip);
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.minDistance = minRange;
            audioSource.maxDistance = maxRange;

            // If we want to attach the audio source to something specific, do that. Otherwise, detach it from any parents.
            // This is useful for things that are obviously creating the sound, like a mouse's squeak
            // we don't want the mouse to leave the squeak behind as it travels, but a flying soda can making a sound at the site of impact is probably fine.
            audioSource.transform.parent = parent == null ? null : parent.transform;
            audioSource.loop = isLooping;
            audioSource.Play();
        }

        [ObserversRpc]
        public void RpcStopAudioSource(NetworkObject parent)
        {
            AudioSource audioSource = parent.GetComponentInChildren<AudioSource>();

            if (audioSource)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Finds an available audio source in the list of available audio sources. If one does not exist, it creates one.
        /// </summary>
        public AudioSource FindAvailableAudioSource(AudioType audioType)
        {
            AudioSource validSource = null;

            AudioSourcesList audioSources = _audioSourcesLists.Find(x => x.AudioType == audioType);
            audioSources.List.RemoveAll(source => source == null);

            // If there are no audio sources in our list, fix that.
            if (audioSources.List.Count == 0)
            {
                audioSources.CreateNewAudioSource();
            }

            // Check the list for an audio source that isn't being used.
            foreach (AudioSource source in audioSources.List)
            {
                if (source != null && !source.isPlaying)
                {
                    // If we found one, exit the foreach loop.
                    validSource = source;

                    break;
                }
            }

            // If we have gone through the list and there's no available ones...
            if (!validSource)
            {
                audioSources.CreateNewAudioSource();

                // And let's use it.
                validSource = audioSources.List[audioSources.List.Count - 1];
            }

            return validSource;
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            Subsystems.Register(this);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            Subsystems.Unregister(this);
        }

        protected void Start()
        {
            CreateAudioSourceIndex();
            StartCoroutine(PurgeCountdown());
        }

        /// <summary>
        /// Creates a list of audio sources, and instantiates our minimum number.
        /// </summary>
        private void CreateAudioSourceIndex()
        {
            _audioSourcesLists = new List<AudioSourcesList>
            {
                new(_maxSfxAudioSources, _minSfxAudioSources, AudioType.Sfx, _sfxAudioSourcePrefab, GameObject),
                new(_maxMusicAudioSources, _minMusicAudioSources, AudioType.Music, _musicAudioSourcePrefab, GameObject),
            };
        }

        /// <summary>
        /// Waits a specific amount of time before purging unused audio sources.
        /// </summary>
        private IEnumerator PurgeCountdown()
        {
            // We want our wait period in minutes, so multiply by 60.
            yield return new WaitForSeconds(_purgeFrequency * 60);

            _audioSourcesLists.Where(x => x.List.Count > x.MaxAudioSources).ToList().ForEach(sourcelist => sourcelist.PurgeUnusedAudioSources());

            StartCoroutine(PurgeCountdown());
        }
    }
}
