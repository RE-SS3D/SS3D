using UnityEngine;
using Mirror;

public class NoisyCollision : MonoBehaviour
{

    //Variables!!! Wow!
    [Header("Collision Noises Setup")]
    [Range(0f, 1f)]
    [Tooltip("How loud sounds will play when colliding.")]
    public float collisionVolume = 0.7f;
    [Tooltip("How fast this object must hit another in order to make a light impact sound.")]
    public float lightImpactVelocity = 1;
    [Tooltip("Does this object make a different sound being struck at a high velocity?")]
    public bool useHardImpactSounds = false;
    [Tooltip("How fast this object must hit another in order to make a hard impact sound.")]
    public float hardImpactVelocity = 7.5f;
    [Tooltip("List of possible sounds that will play when this object collides lightly.")]
    public AudioClip[] lightImpactSounds;
    [Tooltip("List of possible sounds that will play when this object collides heavily.")]
    public AudioClip[] hardImpactSounds;
    
    
    
    //For some reason, this is needed to have an enable/disable feature. Peculiar.
    private void FixedUpdate() {    
    }

    private void OnValidate()
    {
        //Throw a warning if the user configured it retardedly.
        if((lightImpactVelocity > hardImpactVelocity) || (useHardImpactSounds && hardImpactSounds == null) || (lightImpactSounds == null))
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
    
    // Send an event to the server that says we need an audio source.
    public void PlayCollisionSound(AudioClip[] soundPool)
    {
        AudioManager.Instance.PlayAudioSource(PickSound(soundPool), gameObject.transform.position, null, collisionVolume, 1f, 1f, 500f);
    }
    
    public AudioClip PickSound(AudioClip[] availableSounds){
        //Pick a clip from the supplied array and return it
        AudioClip currentClip = availableSounds[UnityEngine.Random.Range(0, availableSounds.Length)];
        return currentClip;
    }
}
