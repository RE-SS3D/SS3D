using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Enums;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace SS3D.Systems.Audio
{
    public class AudioSystem : Core.Behaviours.NetworkSystem
    {
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

        [Tooltip("The SFX audio source to be spawned.")]
        public GameObject _sfxAudioSourcePrefab;

        [Tooltip("The music audio source to be spawned.")]
        public GameObject _musicAudioSourcePrefab;


        [Tooltip("The number of SFX audio sources to start out with.")]
        public int _minSfxAudioSources = 10;
        [Tooltip("Any more than this will be considered too many SFX audio sources.")]
        public int _maxSfxAudioSources = 30;


        [Tooltip("The number of music audio sources to start out with.")]
        public int _minMusicAudioSources = 3;
        [Tooltip("Any more than this will be considered too many SFX audio sources.")]
        public int _maxMusicAudioSources = 10;

        [Tooltip("How often (in minutes) do we purge unused audio sources?")]
        [SerializeField] private int purgeFrequency = 2;

        private List<AudioSourcesList> audioSourcesLists;

        private void Start()
        {
            CreateAudioSourceIndex();
            StartCoroutine(PurgeCountdown());
        }

        /// <summary>
        /// Grabs an unused audio source and plays an audio clip at the desired location.
        /// </summary>
        [Server]
        public void PlayAudioSource(AudioType audioType, AudiosIds audioClipId, Vector3 position)
        {
            PlayAudioSource(audioType, audioClipId, position, NetworkObject);
        }

        /// <summary>
        /// Grabs a free audio source and parents it to a specific object before playing it.
        /// </summary>
        [Server]
        public void PlayAudioSource(AudioType audioType, AudiosIds audioClipId, NetworkObject parent)
        {
            RpcPlayAudioSource(audioType, audioClipId, parent.transform.position, parent);
        }

        /// <summary>
        /// Plays a sound clip at a position, parent, with specific volume, pitch, and ranges.
        /// Volume, pitch, and ranges are optional.
        /// </summary>
        [Server]
        public void PlayAudioSource(AudioType audioType, AudiosIds audioClipId, Vector3 position, NetworkObject parent, float volume = 0.7f, float pitch = 1f, float minRange = 1f, float maxRange = 500f)
        {
            RpcPlayAudioSource(audioType, audioClipId, position, parent, volume, pitch, minRange, maxRange);   
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
        public void RpcPlayAudioSource(AudioType type, AudiosIds audioClipId, Vector3 position, NetworkObject parent, float volume = 0.7f, float pitch = 1f, float minRange = 1f, float maxRange = 500f)
        {
            var audioClip = Assets.Get<AudioClip>((int)AssetDatabases.Audios, (int)audioClipId);
            var audioSource = FindAvailableAudioSource(type);
            audioSource.gameObject.transform.position = position;
            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.minDistance = minRange;
            audioSource.maxDistance = maxRange;
            //If we want to attach the audio source to something specific, do that. Otherwise, detach it from any parents.
            //This is useful for things that are obviously creating the sound, like a mouse's squeak
            //-- we don't want the mouse to leave the squeak behind as it travels, but a flying soda can making a sound at the site of impact is probably fine.
            audioSource.transform.parent = parent == null ? null : parent.transform;
            audioSource.Play();
        }

        [ObserversRpc]
        public void RpcStopAudioSource(NetworkObject parent)
        {
            parent.GetComponentInChildren<AudioSource>().Stop();
        }

        /// <summary>
        /// Finds an available audio source in the list of available audio sources. If one does not exist, it creates one.
        /// </summary>
        public AudioSource FindAvailableAudioSource(AudioType audioType)
        {
            AudioSource validSource = null;

            AudioSourcesList audioSources = audioSourcesLists.Find(x => x.audioType == audioType);

            //If there are no audio sources in our list, fix that.
            if (audioSources.list.Count == 0)
            {
                audioSources.CreateNewAudioSource();
            }

            //Check the list for an audio source that isn't being used.
            foreach (AudioSource source in audioSources.list)
            {
                if (!source.isPlaying)
                {
                    //If we found one, exit the foreach loop.
                    validSource = source;
                    break;
                }
            }

            //If we have gone through the list and there's no available ones...
            if (validSource == null)
            {
                audioSources.CreateNewAudioSource();
                //And let's use it.
                validSource = audioSources.list[audioSources.list.Count - 1];
            }

            return validSource;
        }

        /// <summary>
        /// Creates a list of audio sources, and instantiates our minimum number.
        /// </summary>
        private void CreateAudioSourceIndex()
        {
            audioSourcesLists = new List<AudioSourcesList>
            {
                new AudioSourcesList(_maxSfxAudioSources, _minSfxAudioSources, AudioType.sfx, _sfxAudioSourcePrefab, GameObject),
                new AudioSourcesList(_maxMusicAudioSources, _minMusicAudioSources, AudioType.music, _musicAudioSourcePrefab, GameObject)
            };
        }

        /// <summary>
        /// Waits a specific amount of time before purging unused audio sources.
        /// </summary>
        IEnumerator PurgeCountdown()
        {
            // We want our wait period in minutes, so multiply by 60.
            yield return new WaitForSeconds(purgeFrequency * 60);

            audioSourcesLists.Where(x => x.list.Count > x.maxAudioSources)
                .ToList()
                .ForEach(sourcelist => sourcelist.PurgeUnusedAudioSources());

            StartCoroutine(PurgeCountdown());
        }

        private struct AudioSourcesList
        {
            public int maxAudioSources;
            public int minAudioSources;
            public AudioType audioType;
            public List<AudioSource> list;
            public GameObject prefab;
            public GameObject audioSystem;

            public AudioSourcesList(int maxSources, int minSources, AudioType type, GameObject audioSourcePrefab, GameObject audioSystemGameObject)
            {
                audioType = type;
                list = new List<AudioSource>();
                minAudioSources = minSources;
                maxAudioSources = maxSources;
                prefab = audioSourcePrefab;
                audioSystem= audioSystemGameObject;

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
                AudioSource newAudioSource = Instantiate(prefab, audioSystem.transform.position, Quaternion.identity).GetComponent<AudioSource>();
                list.Add(newAudioSource);
                newAudioSource.transform.parent = audioSystem.transform;
            }

            /// <summary>
            /// Destroys all audio sources that aren't currently playing and removes them from the list.
            /// </summary>
            public void PurgeUnusedAudioSources()
            {
                foreach (var source in list)
                {
                    //Check that the audio source is idle, and we have more than our minimum number.
                    if (!source.isPlaying && list.Count > minAudioSources)
                    {
                        list.Remove(source);
                        source.gameObject.Dispose(true);
                    }
                }
            }
        }

    }
}

