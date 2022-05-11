using UnityEngine;
using Mirror;

namespace SS3D.Engine.Audio.Networking
{
    //TODO: Find a way to tell the audio manager to play a desired AudioClip. John said refer to a listing in a dictionary.
    
    /// <summary>
    /// Tell the network we need an audio source to play here.
    /// </summary>
    public struct RequestAudioSource : NetworkMessage
    {
        public string clipName;
        public Vector3 position;
    }
    /// <summary>
    /// Tell the network we need an audio source to be parented to this object.
    /// </summary>
    public struct TakeAudioSource : NetworkMessage
    {
        public string clipName;
        public GameObject parent;
    }
    /// <summary>
    /// Tell the network we need an audio source to play with specific parameters.
    /// </summary>
    public struct RequestAudioSourceAdvanced : NetworkMessage
    {
        public string clipName;
        public Vector3 position;
        public GameObject parent;
        public float volume;
        public float pitch;
        public float minRange;
        public float maxRange;
    }
}
