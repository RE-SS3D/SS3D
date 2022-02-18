using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoisyCollision : MonoBehaviour
{

    [Range(0f, 1f)]
    [Tooltip("How loud sounds will play when colliding.")]
    public float collisionVolume = 0.2f;
    [Tooltip("How fast this object must hit another in order to make a light impact sound.")]
    public float lightImpactVelocity = 1;
    [Tooltip("Does this object make a different sound being struck at a high velocity?")]
    public bool useHardImpactSounds = false;
    [Tooltip("How fast this object must hit another in order to make a hard impact sound.")]
    public float hardImpactVelocity = 7.5f;
    [Tooltip("This object's audio source.")]
    public AudioSource audioSource;
    [Tooltip("Do we need a second audio source just in case this object collides rapidly?")]
    public bool useBackupAudioSource;
    [Tooltip("Backup audio source used for playing collision sounds in rapid succession.")]
    public AudioSource backupAudioSource;
    [Tooltip("List of possible sounds that will play when this object collides lightly.")]
    public AudioClip[] lightImpactSounds;
    [Tooltip("List of possible sounds that will play when this object collides heavily.")]
    public AudioClip[] hardImpactSounds;

    //For some reason, this is needed to have an enable/disable feature.
    private void FixedUpdate() {    
    }

    void OnCollisionEnter(Collision other) {
        //We have collided. Before anything, be sure that these aren't true...
            //Light impact velocity is lower than hard impact velocity
                                                        //useHardImpactSounds is enabled, but there are no hard impact sounds
                                                                                                                //We are missing light impact sounds
                                                                                                                                                //We have no audio source
                                                                                                                                                                        //The backup audio source is active, yet does not exist.
        if(!(lightImpactVelocity > hardImpactVelocity) && !(useHardImpactSounds && hardImpactSounds == null) && !(lightImpactSounds == null) && !(audioSource == null) && !(useBackupAudioSource && backupAudioSource == null))
        {
            //If we're using hard impact sounds and we are travelling fast enough to play one, do so.
            if(useHardImpactSounds && other.relativeVelocity.magnitude > hardImpactVelocity){

                //Should we try to use backup audio source?
                if(useBackupAudioSource && audioSource.isPlaying && !backupAudioSource.isPlaying){
                    backupAudioSource.PlayOneShot(PickSound(hardImpactSounds), collisionVolume);
                }
                //If not, try normal audio source.
                else if(!audioSource.isPlaying){
                    audioSource.PlayOneShot(PickSound(hardImpactSounds), collisionVolume);
                }
                
            }
            //If no hard collision, can we play a light collision sound?
            else if(other.relativeVelocity.magnitude > lightImpactVelocity){

                //Should we try to use backup audio source?
                if(useBackupAudioSource && audioSource.isPlaying && !backupAudioSource.isPlaying){
                    backupAudioSource.PlayOneShot(PickSound(lightImpactSounds), collisionVolume);
                }
                //If not, try normal audio source.
                else if(!audioSource.isPlaying){
                    audioSource.PlayOneShot(PickSound(lightImpactSounds), collisionVolume);
                }
            }
        }
        //If something is odd, throw an error.
        else{
            Debug.LogWarning("<color=red>Woops!</color> " + gameObject.name + " is trying to play a collision sound but can't. Make sure the Noisy Collision component is configured correctly.");
        }
    }

    public AudioClip PickSound(AudioClip[] availableSounds){
        //Pick a clip from the supplied array and return it
        AudioClip currentClip = availableSounds[Random.Range(0, availableSounds.Length)];
        return currentClip;
    }

}
