using System.Collections.Generic;
using UnityEngine;
using Mirror;
using SS3D.Engine.Audio.Networking;

public class AudioManager : NetworkBehaviour
    {
        [Tooltip("The audio source to be spawned.")]
        public GameObject audioSourcePrefab;
        [Tooltip("The number of audio sources to start out with.")]
        public int minAudioSources = 10;
        [Tooltip("The full list of audio sources -- just a helpful indicator of how many there are.")]
        public List<AudioSource> audioSources;
        
        
        [Header(" - !!!PLACEHOLDER STUFF!!! - ")]
        private Dictionary<string, AudioClip> audioClipIndex;
        [SerializeField]
        private AudioClip clip1;
        [SerializeField]
        private AudioClip clip2;
        
        private void Start()
        {
            audioSources = new List<AudioSource>();
            
            //Instantiate the minimum number of audio sources.
            for (var i = 0; i < minAudioSources; i++)
            {
                CreateNewAudioSource();
            }

            // TODO: Make sure we have a way to collect all possible audio clips in the dictionary. For now, this will do:
            audioClipIndex = new Dictionary<string, AudioClip>();
            
            audioClipIndex.Add("bikehorn", clip1);
            audioClipIndex.Add("can1", clip2);
            
        }
        
        
        // Networking junk -- AudioManager needs to listen to these network events.
        private void SubscribeToEvents()
        {
            NetworkServer.RegisterHandler<RequestAudioSource>(PlayAudioSource);
            NetworkServer.RegisterHandler<TakeAudioSource>(GiveAudioSource);
            NetworkServer.RegisterHandler<RequestAudioSourceAdvanced>(PlayAudioSourceAdvanced);
        }

        /// <summary>
        /// Grabs an unused audio source and plays an audio clip at the desired location.
        /// </summary>
        private void PlayAudioSource(NetworkConnection sender, RequestAudioSource audioSourceRequest)
        {
            PlayAudioSourceFullParams(GetAudioClip(audioSourceRequest.clipName), audioSourceRequest.position, gameObject, 0.7f, 1f, 1f, 500f);
        }

        /// <summary>
        /// Grabs a free audio source and parents it to a specific object before playing it.
        /// </summary>
        private void GiveAudioSource(NetworkConnection sender, TakeAudioSource audioSourceRequest)
        {
            PlayAudioSourceFullParams(GetAudioClip(audioSourceRequest.clipName), audioSourceRequest.parent.transform.position, audioSourceRequest.parent, 0.7f, 1f, 1f, 500f);
        }
        
        /// <summary>
        /// Plays a sound clip at a position, parent, with specific volume, pitch, and ranges.
        /// Default float values are 0.7f, 1f, 1f, and 500f respectively. 
        /// </summary>
        private void PlayAudioSourceAdvanced(NetworkConnection sender, RequestAudioSourceAdvanced audioSourceRequest)
        {
            PlayAudioSourceFullParams(GetAudioClip(audioSourceRequest.clipName), audioSourceRequest.position, audioSourceRequest.parent, audioSourceRequest.volume, audioSourceRequest.pitch, audioSourceRequest.minRange, audioSourceRequest.maxRange);
        }
        
        /// <summary>
        /// Delivers an audio source using paramaters passed into it.
        /// </summary>
        private void PlayAudioSourceFullParams(AudioClip audioClip, Vector3 position, GameObject parent, float volume, float pitch, float minRange, float maxRange)
        {
            var audioSource = FindAvailableAudioSource();
            audioSource.gameObject.transform.position = position;
            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.minDistance = minRange;
            audioSource.maxDistance = maxRange;
            //If we want to attach the audio source to something specific, do that. Otherwise, detach it from any parents.
            //This is useful for things that are obviously creating the sound, like a mouse's squeak -- we don't want the mouse to leave the squeak behind as it travels, but a flying soda can making a sound at the site of impact is probably fine.
            audioSource.transform.parent = parent == null ? null : parent.transform;
            audioSource.Play();
        }

        /// <summary>
        /// Finds an available audio source in the list of available audio sources. If one does not exist, it creates one.
        /// </summary>
        private AudioSource FindAvailableAudioSource()
        {
            AudioSource validSource = null;
            
            //If there are no audio sources in our list, fix that.
            if (audioSources.Count == 0)
            {
                CreateNewAudioSource();
            }
            
            //Check the list for an audio source that isn't being used.
            foreach (AudioSource source in audioSources)
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
                CreateNewAudioSource();
                //And let's use it.
                validSource = audioSources[audioSources.Count - 1];
            }
            
            return validSource;
        }

        /// <summary>
        /// Creates a new audio source and adds it to the list.
        /// </summary>
        private void CreateNewAudioSource()
        {
            var newAudioSource = Instantiate(audioSourcePrefab, gameObject.transform.position, Quaternion.identity).GetComponent<AudioSource>();
            newAudioSource.transform.parent = gameObject.transform;
            audioSources.Add(newAudioSource);
        }
        
        /// <summary>
        /// Destroys all audio sources that aren't currently playing and removes them from the list.
        /// </summary>
        public void PurgeUnusedAudioSources()
        {
            foreach (var source in audioSources)
            {
                if (!source.isPlaying)
                {
                    audioSources.Remove(source);
                    Destroy(source.gameObject);
                }
            }
        }

        public AudioClip GetAudioClip(string clipName)
        {
            
            AudioClip result = clip1;

            foreach (var listing in audioClipIndex)
            {
                //Check to see if the key is what was requested.
                if (listing.Key == clipName)
                {
                    result = listing.Value;
                }
            }
            
            return result;
        }
    }

