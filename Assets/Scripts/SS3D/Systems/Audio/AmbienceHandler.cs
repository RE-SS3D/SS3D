using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AmbienceHandler : MonoBehaviour
{
    
    [Header("Current Status")]
    
    //TODO: Change this based on how much air is in the room.
    [Tooltip("How much air is in the room. This effects the volume of the audio sources.")]
    [Range(0f,1f)] public float air = 1;
    //TODO: Change this based on air flow in the room.
    [Tooltip("How windy the air is in the area. This effects how the air sounds.")]
    [Range(0f, 1f)] public float windiness;
    //TODO: Change this based on area's power.
    [Tooltip("The amount of power in the area. Lower levels means quieter and deeper electrical humming.")]
    [Range(0f, 1f)] public float power = 1;
    [Tooltip("What clip is currently being played by the ambient noise generator. You don't need to mess with this, just for debugging purposes.")] 
    [SerializeField] private AudioClip nowPlaying;

    [Header("Audio Source Setup")]
    
    //We need three wind-related audio sources so we can seamlessly shift between them. Don't worry, they get disabled when they aren't needed.
    [Tooltip("The audio source that will be playing air noises.")]
    public AudioSource airPlayer;
    [Tooltip("The audio source that will be playing light wind noises.")]
    public AudioSource lightWindPlayer;
    [Tooltip("The audio source that will be playing heavy wind noises.")]
    public AudioSource heavyWindPlayer;
    [Tooltip("The audio source that will be playing electrical noises.")]
    public AudioSource electricalPlayer;
    [Tooltip("The audio source that will be playing ambient noises.")]
    public AudioSource noisePlayer;
    
    [Header("Audio Clip Setup")]
    [Tooltip("How often (in seconds) an ambient noise should attempt to play.")]
    public int ambientNoiseFrequency = 3;
    [Tooltip("How likely (percentage) it is that an ambient noise will play.")]
    [Range(0,100)] public int ambientNoiseChance = 75;
    //TODO: Create a way to get the environment to tell us what noises it makes. Would be useful for off-station or room-based ambience.
    [Tooltip("Sounds that will be made by the environment -- for example, general station noises.")]
    public AudioClip[] environmentNoises;
    [Tooltip("Sounds that will play when the amount of air is equal to zero.")]
    public AudioClip[] spaceNoises;

    [Header("Animator Stuff")] 
    public Animator sfxAnimator;
    [Tooltip("Value of lowpass cutoff in SFX mixer group. Attenuated by the animator.")]
    [Range(480f, 22000f)] public float sfxLowPassCutoff = 22000f;
    [Tooltip("Value of pitch boosting in SFX mixer group. Attenuated by the animator.")]
    [Range(1f, 2f)] public float sfxPitchBoost = 1;
    [Tooltip("Value of pitch shifting in SFX mixer group. Attenuated by the animator.")]
    [Range(0.8f, 1f)] public float sfxPitchShift = 1;
    [Tooltip("The mixer that we need to look at to grab our effects.")]
    public AudioMixer masterMixer;
    
        private void Start()
    {
        if (ambientNoiseFrequency != 0)
        {
            PlayAmbience();            
        }
    }

    private void Update()
    {
        AttenuateWindSounds();
        AttenuatePowerSounds();
        MuffleSFX();
    }

    /// <summary>
    /// If there is wind, activate the wind audio sources and adjust the sound. If not, disable the audio sources.
    /// </summary>
    private void AttenuateWindSounds()
    {
        //Stagnant air volume should decrease by the amount of windiness
        airPlayer.volume = air - windiness;
        
        if (windiness != 0)
        {
            lightWindPlayer.enabled = true;
            heavyWindPlayer.enabled = true;
            //light wind should peak at .5 windiness, effected by the amount of air. Less air = less loud.
            lightWindPlayer.volume = Mathf.Clamp(windiness * 2, 0f, 1f) * air;
            //Heavy wind should be fully loud at max windiness with max air. Less air = less loud.
            heavyWindPlayer.volume = windiness * air;           
        }
        else
        {
            lightWindPlayer.enabled = false;
            heavyWindPlayer.enabled = false;
        }
    }

    /// <summary>
    /// Attenuates volume and pitch of station's power humming based on how much power is in the area.
    /// </summary>
    private void AttenuatePowerSounds()
    {
        electricalPlayer.volume = power * air;
        electricalPlayer.pitch = power;
    }
    
    
    /// <summary>
    /// Muffles SFX channel based on the amount of air.
    /// </summary>
    private void MuffleSFX()
    {
    sfxAnimator.SetFloat("air", air);
    masterMixer.SetFloat("SFXcutoffFreq", sfxLowPassCutoff);
    masterMixer.SetFloat("SFXlowEQgain", sfxPitchBoost);
    masterMixer.SetFloat("SFXpitch", sfxPitchShift);
    }
    
    /// <summary>
    /// Plays an ambient sound.
    /// </summary>
    private void PlayAmbience()
    {
        Debug.Log("Playing ambience.");
        if (air == 0f)
        {
            nowPlaying = PickSound(spaceNoises);
        }
        else
        {
            nowPlaying = PickSound(environmentNoises);
        }
        noisePlayer.clip = nowPlaying;
        noisePlayer.Play();
        StartCoroutine(AmbientNoiseTimer());
    }
    
    /// <summary>
    /// Grabs a clip from the supplied array.
    /// </summary>
    private AudioClip PickSound(AudioClip[] availableSounds){
        //Pick a clip from the supplied array and return it
        AudioClip currentClip = availableSounds[Random.Range(0, availableSounds.Length)];
        return currentClip;
    }

    /// <summary>
    /// Waits a certain amount of time until playing ambience.
    /// </summary>
    IEnumerator AmbientNoiseTimer()
    {
        yield return new WaitForSeconds(ambientNoiseFrequency);
        // If we generate a number bigger than our chance, then the attempt fails, and we try again in x minutes.
        if (Random.Range(1, 101) < ambientNoiseChance)
        {
            PlayAmbience();
        }
        else
        {
            StartCoroutine(AmbientNoiseTimer());
        }
        
    }
}
