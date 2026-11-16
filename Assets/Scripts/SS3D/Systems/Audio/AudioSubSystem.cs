using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Generated;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Audio system handle creating and destroying audio sources, whether for SFX or music. It handle placing them at the right place, playing and stopping them.
    /// In the vast majority of cases you should use this to play sounds. 
    /// </summary>
    public class AudioSubSystem : NetworkSubSystem
    {
        [Tooltip("The SFX audio source to be spawned.")]
        public GameObject SfxAudioSourcePrefab;

        [Tooltip("The music audio source to be spawned.")]
        public GameObject MusicAudioSourcePrefab;

        [Tooltip("The number of SFX audio sources to start out with.")]
        public int MinSfxAudioSources = 10;
        
        [Tooltip("Any more than this will be considered too many SFX audio sources.")]
        public int MaxSfxAudioSources = 30;

        [Tooltip("The number of music audio sources to start out with.")]
        public int MinMusicAudioSources = 3;

        [Tooltip("Any more than this will be considered too many SFX audio sources.")]
        public int MaxMusicAudioSources = 10;

        [Tooltip("How often (in minutes) do we purge unused audio sources?")]
        [SerializeField]
        private int _purgeFrequency = 2;

        private const float CollisionMinRange = 2f;
        private const float CollisionMaxRange = 30f;

        private readonly CollisionSoundLimiter _collisionLimiter = new();

        private List<AudioSourcesList> _audioSourcesLists;

        protected override void OnAwake()
        {
            base.OnAwake();
            SubSystems.Register(this);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            SubSystems.Unregister(this);
        }

        private void Start()
        {
#if !UNITY_SERVER
            // Audio playback (RpcPlayAudioSource et al.) is [ObserversRpc] and only ever runs on
            // observing clients, so the server never needs its own local AudioSource pool.
            CreateAudioSourceIndex();
            StartCoroutine(PurgeCountdown());
#endif
        }

        /// <summary>
        /// Grabs a free audio source and parents it to a specific object before playing it.
        /// </summary>
        [Server]
        public void PlayAudioSource(AudioType audioType, string audioClipId, NetworkObject parent)
        {
            RpcPlayAudioSource(audioType, audioClipId, parent.transform.position, parent);
        }

        /// <summary>
        /// Plays a sound clip at a position, parent, with specific volume, pitch, and ranges.
        /// Volume, pitch, and ranges are optional.
        /// </summary>
        [Server]
        public void PlayAudioSource(AudioType audioType, string audioClipId, Vector3 position, NetworkObject parent,
            bool isLooping = false, float volume = 0.7f, float pitch = 1f, float minRange = 1f, float maxRange = 500f)
        {
            RpcPlayAudioSource(audioType, audioClipId, position, parent, isLooping, volume, pitch, minRange, maxRange);
        }

        /// <summary>
        /// Plays a collision sound for a material unless <see cref="CollisionSoundLimiter"/> holds
        /// it back. <paramref name="sourceReadyTime"/> is the source's own cooldown state.
        /// </summary>
        [Server]
        public void TryPlayCollisionSound(ItemCollisionMaterial material, Vector3 position, float volume,
            ref float sourceReadyTime)
        {
            if (!_collisionLimiter.TryPlay(Time.time, ref sourceReadyTime))
            {
                return;
            }

            string[] clips = ItemCollisionAudioTrackIds.ClipsFor(material);
            PlayAudioSource(AudioType.Sfx, clips[Random.Range(0, clips.Length)], position, null,
                false, volume, Random.Range(0.92f, 1.08f), CollisionMinRange, CollisionMaxRange);
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
        public void RpcPlayAudioSource(AudioType type, string audioClipID, Vector3 position, NetworkObject parent,
            bool isLooping = false, float volume = 0.7f, float pitch = 1f, float minRange = 1f, float maxRange = 500f)
        {
            PlayAudioSourceLocal(type, audioClipID, position, parent, isLooping, volume, pitch, minRange, maxRange);
        }

        /// <summary>
        /// Plays a clip from the local audio pool. Shared by the observers RPC and by per-object
        /// buffered state RPCs (a machine's looping sound must also reach late observers).
        /// </summary>
        [ServerOrClient]
        public AudioSource PlayAudioSourceLocal(AudioType type, string audioClipID, Vector3 position, NetworkObject parent,
            bool isLooping = false, float volume = 0.7f, float pitch = 1f, float minRange = 1f, float maxRange = 500f,
            float startTime = 0f)
        {
            // The server never builds a pool (see Start); only clients have one.
            if (_audioSourcesLists == null)
                return null;

            AudioSource audioSource = FindAvailableAudioSource(type);

            audioSource.gameObject.transform.position = position;
            audioSource.clip = Assets.Get<AudioClip>(AssetDatabases.Sounds, audioClipID);
            if (audioSource.clip == null)
            {
                // Missing Sounds.asset ref (common after hand-adding files) — don't Play() a null clip.
                return null;
            }

            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.minDistance = minRange;
            audioSource.maxDistance = maxRange;
            // Station props animate (airlocks, etc.); Doppler on a parented pooled source warbles
            // the attack of the clip into a compression-like pitch scoop.
            audioSource.dopplerLevel = 0f;

            //If we want to attach the audio source to something specific, do that. Otherwise, detach it from any parents.
            //This is useful for things that are obviously creating the sound, like a mouse's squeak
            //-- we don't want the mouse to leave the squeak behind as it travels, but a flying soda can making a sound at the site of impact is probably fine.
            audioSource.transform.parent = parent == null ? null : parent.transform;
            audioSource.loop = isLooping;

            // Reset any muffle state left over from this pooled source's previous clip before it
            // starts sampling occlusion fresh (audio.md §3 — client-local, seventh LineOfSight consumer).
            audioSource.GetComponent<AudioSourceOcclusion>()?.PrepareForPlayback(volume);

            audioSource.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, audioSource.clip.length - 0.001f));
            audioSource.Play();
            return audioSource;
        }

        [ObserversRpc]
        public void RpcStopAudioSource(NetworkObject parent)
        {
            StopAudioSourceLocal(parent);
        }

        /// <summary>
        /// Stops the audio source parented to <paramref name="parent"/>, if this client has one.
        /// </summary>
        [ServerOrClient]
        public void StopAudioSourceLocal(NetworkObject parent)
        {
            AudioSource audioSource = parent.GetComponentInChildren<AudioSource>();
            audioSource?.Stop();
        }

        /// <summary>
        /// Finds an available audio source in the list of available audio sources. If one does not exist, it creates one.
        /// </summary>
        public AudioSource FindAvailableAudioSource(AudioType audioType)
        {
            AudioSource validSource = null;

            AudioSourcesList audioSources = _audioSourcesLists.Find(x => x.AudioType == audioType);
            audioSources.List.RemoveAll(source => source == null);

            //If there are no audio sources in our list, fix that.
            if (audioSources.List.Count == 0)
            {
                audioSources.CreateNewAudioSource();
            }

            //Check the list for an audio source that isn't being used.
            foreach (AudioSource source in audioSources.List)
            {
                if (source != null && !source.isPlaying)
                {
                    //If we found one, exit the foreach loop.
                    validSource = source;

                    break;
                }
            }

            //If we have gone through the list and there's no available ones...
            if (!validSource)
            {
                audioSources.CreateNewAudioSource();

                //And let's use it.
                validSource = audioSources.List[audioSources.List.Count - 1];
            }

            return validSource;
        }

        /// <summary>
        /// Creates a list of audio sources, and instantiates our minimum number.
        /// </summary>
        private void CreateAudioSourceIndex()
        {
            _audioSourcesLists = new List<AudioSourcesList>
            {
                new(MaxSfxAudioSources, MinSfxAudioSources, AudioType.Sfx, SfxAudioSourcePrefab, GameObject), 
                new(MaxMusicAudioSources, MinMusicAudioSources, AudioType.Music, MusicAudioSourcePrefab, GameObject)
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

                // Sfx only — Music (jukebox) must not get the 800Hz occlusion lowpass or it reads
                // as permanently muddled even in the same room (furniture/wall false hits on Default).
                if (AudioType is AudioType.Sfx or AudioType.Music)
                {
                    AudioSourceOcclusion occlusion = newAudioSource.gameObject.AddComponent<AudioSourceOcclusion>();
                    occlusion.IgnoresWalls = AudioType is AudioType.Music;
                }

                List.Add(newAudioSource);
                newAudioSource.transform.parent = AudioSystem.transform;
            }

            /// <summary>
            /// Destroys all audio sources that aren't currently playing and removes them from the list.
            /// </summary>
            public void PurgeUnusedAudioSources()
            {
                PurgeIdleAudioSources(List, MinAudioSources);
            }
        }

        /// <summary>
        /// Removes destroyed sources from <paramref name="sources"/>, then destroys idle ones until only
        /// <paramref name="minSources"/> are left. Playing sources are always kept.
        /// </summary>
        /// <returns>How many entries were removed.</returns>
        public static int PurgeIdleAudioSources(List<AudioSource> sources, int minSources)
        {
            int removed = sources.RemoveAll(source => source == null);

            // Backwards, so removing an entry doesn't skip the next one.
            for (int i = sources.Count - 1; i >= 0 && sources.Count > minSources; i--)
            {
                AudioSource source = sources[i];
                if (source.isPlaying)
                {
                    continue;
                }

                sources.RemoveAt(i);
                source.gameObject.Dispose(true);
                removed++;
            }

            return removed;
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Audio Clip Reference")]
        private static void CreateAudioClipReference()
        {
            AudioClip[] selectedClips = UnityEditor.Selection.GetFiltered<AudioClip>(SelectionMode.Assets);

            foreach (AudioClip selectedClip in selectedClips)
            {
                ObjectAssetReference.Create(selectedClip);
            }
        }
#endif
    }
}

