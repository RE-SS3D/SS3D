using UnityEngine;
using SS3D.Data;
using SS3D.Data.Enums;
using FishNet.Object;
using SS3D.Core;
using FishNet;

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
        [SerializeField, Tooltip("How loud sounds will play when colliding.")]
        private float collisionVolume = 0.7f;

        [SerializeField, Tooltip("The base pitch of the sound effect. Default is 1, lower pitch is 0, higher pitch is 2.")]
        [Range(0, 2)] private float basePitch = 1;

        [SerializeField, Tooltip("How much lower pitch can the sound play?")]
        [Range(0f, 0.5f)] private float pitchModulationLow = 0;

        [SerializeField, Tooltip("How much higher pitch can the sound play?")]
        [Range(0f, 0.5f)] private float pitchModulationHigh = 0;

        [SerializeField, Tooltip("How fast this object must hit another in order to make a light impact sound.")]
        private float lightImpactVelocity = 1;

        [SerializeField, Tooltip("Does this object make a different sound being struck at a high velocity?")]
        private bool useHardImpactSounds = false;

        [SerializeField, Tooltip("How fast this object must hit another in order to make a hard impact sound.")]
        private float hardImpactVelocity = 7.5f;

        [SerializeField, Tooltip("List of possible sounds that will play when this object collides lightly.")]
        private SoundsIds[] lightImpactSounds;

        [SerializeField, Tooltip("List of possible sounds that will play when this object collides heavily.")]
        private SoundsIds[] hardImpactSounds;

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

            if (!InstanceFinder.IsServer) return;

            //Only execute this code if we're supposed to make collision noises.
            if (useHardImpactSounds && other.relativeVelocity.magnitude > hardImpactVelocity) {
                PlayCollisionSound(hardImpactSounds);
            }
            else if (other.relativeVelocity.magnitude > lightImpactVelocity) {
                PlayCollisionSound(lightImpactSounds);
            }
        }

        /// <summary>
        // Play some collision sound from the available sounds with some random pitch change
        /// </summary>
        /// <param name="soundPool"></param>
        [Server]
        private void PlayCollisionSound(SoundsIds[] soundPool)
        {
            float pitch = Random.Range(basePitch - pitchModulationLow, basePitch + pitchModulationHigh);
            Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.sfx, PickSound(soundPool), gameObject.transform.position, null,
                false, collisionVolume, pitch);
        }

        private SoundsIds PickSound(SoundsIds[] availableSounds) {
            //Pick a clip from the supplied array and return it
            SoundsIds currentClip = availableSounds[Random.Range(0, availableSounds.Length)];
            return currentClip;
        }
    }
}
