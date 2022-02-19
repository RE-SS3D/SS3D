using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoisyCollision : MonoBehaviour
{

    [ContextMenuItem("Destroy Audio Sources", "DestroyAudioSources")]
    [Header("Collision Noises Setup")]
    [Tooltip("Does this item have collision noises?")]
    public bool makesCollisionNoises = false;
    [Range(0f, 1f)]
    [Tooltip("How loud sounds will play when colliding.")]
    public float collisionVolume = 0.7f;
    [Tooltip("How fast this object must hit another in order to make a light impact sound.")]
    public float lightImpactVelocity = 1;
    [Tooltip("Does this object make a different sound being struck at a high velocity?")]
    public bool useHardImpactSounds = false;
    [Tooltip("How fast this object must hit another in order to make a hard impact sound.")]
    public float hardImpactVelocity = 7.5f;
    [Tooltip("This object's audio source.")]
    [ContextMenuItem("Quick-add audio source", "GenerateAudioSource")]
    public AudioSource audioSource;
    [Tooltip("Do we need a second audio source just in case this object collides rapidly?")]
    public bool useBackupAudioSource;
    [Tooltip("Backup audio source used for playing collision sounds in rapid succession.")]
    [ContextMenuItem("Quick-add audio source", "GenerateBackupAudioSource")]
    public AudioSource backupAudioSource;
    [Tooltip("List of possible sounds that will play when this object collides lightly.")]
    public AudioClip[] lightImpactSounds;
    [Tooltip("List of possible sounds that will play when this object collides heavily.")]
    public AudioClip[] hardImpactSounds;

    //For some reason, this is needed to have an enable/disable feature.
    private void FixedUpdate() {    
    }

    private void OnValidate()
    {
        //Throw a warning if the user configured it retardedly.
        if((lightImpactVelocity > hardImpactVelocity) || (useHardImpactSounds && hardImpactSounds == null) || (lightImpactSounds == null) || (audioSource == null) || (useBackupAudioSource && backupAudioSource == null))
        {
            Debug.LogWarning("<color=red>Woops!</color> " + gameObject.name + " is configured to make collision sounds, but cannot. Make sure the Noisy Collision script is configured correctly.");
            this.enabled = false;
        }
    }
    void OnCollisionEnter(Collision other) {
        //Only execute this code if we're supposed to make collision noises.
        if(useHardImpactSounds && other.relativeVelocity.magnitude > hardImpactVelocity){
            PlayCollisionSound(hardImpactSounds);
        }
        else if(other.relativeVelocity.magnitude > lightImpactVelocity){
            PlayCollisionSound(lightImpactSounds);
        }
    }
    public void PlayCollisionSound(AudioClip[] soundPool)
    {
        //Take the supplied clip and play it through the best available audio source
        if(useBackupAudioSource && audioSource.isPlaying && !backupAudioSource.isPlaying){
            backupAudioSource.PlayOneShot(PickSound(soundPool), collisionVolume);
        }
        else if(!audioSource.isPlaying){
            audioSource.PlayOneShot(PickSound(soundPool), collisionVolume);
        }
    }
    public AudioClip PickSound(AudioClip[] availableSounds){
        //Pick a clip from the supplied array and return it
        AudioClip currentClip = availableSounds[UnityEngine.Random.Range(0, availableSounds.Length)];
        return currentClip;
    }
    private void GenerateAudioSource(){
        if(audioSource == null)
        {
            audioSource = gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;           
        }
    }
    private void GenerateBackupAudioSource(){
        if (backupAudioSource == null)
        {
            backupAudioSource = gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
            backupAudioSource.playOnAwake = false;
            backupAudioSource.spatialBlend = 1f;          
        }
    }
    [ContextMenu("Remove Audio Sources")]
    private void DestroyAudioSources(){
        DestroyImmediate(backupAudioSource, true);
        DestroyImmediate(audioSource, true);
    }
}
