using UnityEngine;
using FishNet.Object;
using SS3D.Core;
using FishNet;
using UnityEngine.Serialization;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Put this script on stuff that make sound when they collide.
    /// </summary>
    public class NoisyCollision : MonoBehaviour
    {

        //Variables!!! Wow!
        [Header("Collision Noises Setup")]
        [Range(0f, 1f)]
        [SerializeField]
        [Tooltip("How loud sounds will play when colliding.")]
        private float _collisionVolume = 0.7f;

        [SerializeField]
        [Range(0, 2)]
        [Tooltip("The base pitch of the sound effect. Default is 1, lower pitch is 0, higher pitch is 2.")]
        private float _basePitch = 1;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("How much lower pitch can the sound play?")]
        private float _pitchModulationLow = 0;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("How much higher pitch can the sound play?")]
        private float _pitchModulationHigh = 0;

        [SerializeField]
        [Tooltip("How fast this object must hit another in order to make a light impact sound.")]
        private float _lightImpactVelocity = 1;

        [SerializeField]
        [Tooltip("Does this object make a different sound being struck at a high velocity?")]
        private bool _useHardImpactSounds = false;

        [SerializeField]
        [Tooltip("How fast this object must hit another in order to make a hard impact sound.")]
        private float _hardImpactVelocity = 7.5f;

        [SerializeField]
        [Tooltip("List of possible sounds that will play when this object collides lightly.")]
        private AudioClip[] _lightImpactSounds;

        [SerializeField]
        [Tooltip("List of possible sounds that will play when this object collides heavily.")]
        private AudioClip[] _hardImpactSounds;

        //For some reason, this is needed to have an enable/disable feature. Peculiar.
        private void FixedUpdate() { }

        private void OnValidate()
        {
            // Throw a warning if the user configured it retardedly.
            if ((!(_lightImpactVelocity > _hardImpactVelocity)) && (!_useHardImpactSounds || _hardImpactSounds != null) && (_lightImpactSounds != null))
            {
                return;
            }

            Debug.LogWarning("<color=red>Woops!</color> " + gameObject.name + " is configured to make collision sounds, but cannot. Make sure the Noisy Collision script is configured correctly.");
            enabled = false;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!InstanceFinder.IsServer)
            {
                return;
            }

            //Only execute this code if we're supposed to make collision noises.
            if (_useHardImpactSounds && other.relativeVelocity.magnitude > _hardImpactVelocity)
            {
                PlayCollisionSound(_hardImpactSounds);
            }
            else if (other.relativeVelocity.magnitude > _lightImpactVelocity)
            {
                PlayCollisionSound(_lightImpactSounds);
            }
        }

        /// <summary>
        /// Play some collision sound from the available sounds with some random pitch change.
        /// </summary>
        /// <param name="soundPool"></param>
        [Server]
        private void PlayCollisionSound(AudioClip[] soundPool)
        {
            float pitch = Random.Range(_basePitch - _pitchModulationLow, _basePitch + _pitchModulationHigh);
            SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Sfx, PickSound(soundPool), gameObject.transform.position, null,
                false, _collisionVolume, pitch);
        }

        private AudioClip PickSound(AudioClip[] availableSounds)
        {
            // Pick a clip from the supplied array and return it
            AudioClip currentClip = availableSounds[Random.Range(0, availableSounds.Length)];

            return currentClip;
        }
    }
}
