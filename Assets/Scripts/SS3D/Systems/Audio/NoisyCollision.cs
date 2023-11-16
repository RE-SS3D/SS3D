using UnityEngine;
using SS3D.Data;
using SS3D.Data.Enums;
using FishNet.Object;
using SS3D.Core;

namespace SS3D.Systems.Audio
{
    public class NoisyCollision : MonoBehaviour
    {

        //Variables!!! Wow!
        [Header("Collision Noises Setup")]
        [Range(0f, 1f)]
        [Tooltip("How loud sounds will play when colliding.")]
        public float collisionVolume = 0.7f;
        [Tooltip("The base pitch of the sound effect. Default is 1, lower pitch is 0, higher pitch is 2.")]
        [Range(0, 2)] public float basePitch = 1;
        [Tooltip("How much lower pitch can the sound play?")]
        [Range(0f, 0.5f)] public float pitchModulationLow = 0;
        [Tooltip("How much higher pitch can the sound play?")]
        [Range(0f, 0.5f)] public float pitchModulationHigh = 0;
        [Tooltip("How fast this object must hit another in order to make a light impact sound.")]
        public float lightImpactVelocity = 1;
        [Tooltip("Does this object make a different sound being struck at a high velocity?")]
        public bool useHardImpactSounds = false;
        [Tooltip("How fast this object must hit another in order to make a hard impact sound.")]
        public float hardImpactVelocity = 7.5f;
        [Tooltip("List of possible sounds that will play when this object collides lightly.")]
        public AudiosIds[] lightImpactSounds;
        [Tooltip("List of possible sounds that will play when this object collides heavily.")]
        public AudiosIds[] hardImpactSounds;





        //For some reason, this is needed to have an enable/disable feature. Peculiar.
        private void FixedUpdate() {
        }

        private void OnValidate()
        {
            //Throw a warning if the user configured it retardedly.
            if ((lightImpactVelocity > hardImpactVelocity) || (useHardImpactSounds && hardImpactSounds == null) || (lightImpactSounds == null))
            {
                Debug.LogWarning("<color=red>Woops!</color> " + gameObject.name + " is configured to make collision sounds, but cannot. Make sure the Noisy Collision script is configured correctly.");
                this.enabled = false;
            }
        }

        void OnCollisionEnter(Collision other) {
            //Only execute this code if we're supposed to make collision noises.
            if (useHardImpactSounds && other.relativeVelocity.magnitude > hardImpactVelocity) {
                PlayCollisionSound(hardImpactSounds);
            }
            else if (other.relativeVelocity.magnitude > lightImpactVelocity) {
                PlayCollisionSound(lightImpactSounds);
            }
        }

        // Send an event to the server that says we need an audio source.
        public void PlayCollisionSound(AudiosIds[] soundPool)
        {
            float pitch = Random.Range(basePitch - pitchModulationLow, basePitch + pitchModulationHigh);
            Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.sfx, PickSound(soundPool), gameObject.transform.position, null, collisionVolume, pitch);
        }

        public AudiosIds PickSound(AudiosIds[] availableSounds) {
            //Pick a clip from the supplied array and return it
            AudiosIds currentClip = availableSounds[Random.Range(0, availableSounds.Length)];
            return currentClip;
        }
    }
}
