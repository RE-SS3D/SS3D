using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Script to deal with playing ambient sounds, including power buzz, wind, creepy background sounds and such.
    /// </summary>
    public class AmbienceHandler : Actor
    {

        [Header("Current Status")]

        //TODO: Change this based on how much air is in the room.
        [Tooltip("How much air is in the room. This effects the volume of the audio sources.")]
        [SerializeField, Range(0f, 1f)] private float _air = 1;

        //TODO: Change this based on air flow in the room.
        [Tooltip("How windy the air is in the area. This effects how the air sounds.")]
        [SerializeField, Range(0f, 1f)] private float _windiness;

        //TODO: Change this based on area's power.
        [Tooltip("The amount of power in the area. Lower levels means quieter and deeper electrical humming.")]
        [SerializeField, Range(0f, 1f)] private float _power = 1;

        [Tooltip("What clip is currently being played by the ambient noise generator. You don't need to mess with this, just for debugging purposes.")]
        [SerializeField] private AudioClip _nowPlaying;

        [Header("Audio Source Setup")]

        //We need three wind-related audio sources so we can seamlessly shift between them. Don't worry, they get disabled when they aren't needed.
        [Tooltip("The audio source that will be playing air noises.")]
        [SerializeField]
        private AudioSource _airPlayer;

        [Tooltip("The audio source that will be playing light wind noises.")]
        [SerializeField]
        private AudioSource _lightWindPlayer;

        [Tooltip("The audio source that will be playing heavy wind noises.")]
        [SerializeField]
        private AudioSource _heavyWindPlayer;

        [Tooltip("The audio source that will be playing electrical noises.")]
        [SerializeField]
        private AudioSource _electricalPlayer;

        [Tooltip("The audio source that will be playing ambient noises.")]
        [SerializeField]
        private AudioSource _noisePlayer;

        [Header("Audio Clip Setup")]
        [SerializeField]
        [Tooltip("How often (in seconds) an ambient noise should attempt to play.")]
        private int _ambientNoiseFrequency = 3;

        [Tooltip("How likely (percentage) it is that an ambient noise will play.")]
        [SerializeField]
        [Range(0, 100)] private int _ambientNoiseChance = 75;

        //TODO: Create a way to get the environment to tell us what noises it makes. Would be useful for off-station or room-based ambience.
        [Tooltip("Sounds that will be made by the environment -- for example, general station noises.")]
        [SerializeField]
        private AudioClip[] _environmentNoises;

        [Tooltip("Sounds that will play when the amount of air is equal to zero.")]
        [SerializeField]
        private AudioClip[] _spaceNoises;

        [Header("Animator Stuff")]
        [SerializeField]
        private Animator _sfxAnimator;

        [Tooltip("Value of lowpass cutoff in SFX mixer group. Attenuated by the animator.")]
        [SerializeField, Range(480f, 22000f)] private float _sfxLowPassCutoff = 22000f;

        [Tooltip("Value of pitch boosting in SFX mixer group. Attenuated by the animator.")]
        [SerializeField, Range(1f, 2f)] private float _sfxPitchBoost = 1;

        [Tooltip("Value of pitch shifting in SFX mixer group. Attenuated by the animator.")]
        [SerializeField, Range(0.8f, 1f)] private float _sfxPitchShift = 1;

        [Tooltip("The mixer that we need to look at to grab our effects.")]
        [SerializeField]
        private AudioMixer _masterMixer;

        private bool _clientHasSpawned;

        protected override void OnStart()
        {
            SubSystems.Get<EntitySubSystem>().OnClientSpawn += HandleClientSpawn;
            SetVolumeOfAll(0f);
        }

        private void Update()
        {
            if (!_clientHasSpawned) return;
            AttenuateWindSounds();
            AttenuatePowerSounds();
            MuffleSFX();
        }

        private void HandleClientSpawn()
        {
            _clientHasSpawned = true;
            if (_ambientNoiseFrequency != 0)
            {
                PlayAmbience();
            }   
        }

        /// <summary>
        /// If there is wind, activate the wind audio sources and adjust the sound. If not, disable the audio sources.
        /// </summary>
        private void AttenuateWindSounds()
        {
            //Stagnant air volume should decrease by the amount of windiness
            _airPlayer.volume = _air - _windiness;

            if (_windiness != 0)
            {
                _lightWindPlayer.enabled = true;
                _heavyWindPlayer.enabled = true;
                //light wind should peak at .5 windiness, effected by the amount of air. Less air = less loud.
                _lightWindPlayer.volume = Mathf.Clamp(_windiness * 2, 0f, 1f) * _air;
                //Heavy wind should be fully loud at max windiness with max air. Less air = less loud.
                _heavyWindPlayer.volume = _windiness * _air;
            }
            else
            {
                _lightWindPlayer.enabled = false;
                _heavyWindPlayer.enabled = false;
            }
        }

        /// <summary>
        /// Attenuates volume and pitch of station's power humming based on how much power is in the area.
        /// </summary>
        private void AttenuatePowerSounds()
        {
            _electricalPlayer.volume = _power * _air;
            _electricalPlayer.pitch = _power;
        }


        /// <summary>
        /// Muffles SFX channel based on the amount of air.
        /// </summary>
        private void MuffleSFX()
        {
            _sfxAnimator.SetFloat("air", _air);
            _masterMixer.SetFloat("SFXcutoffFreq", _sfxLowPassCutoff);
            _masterMixer.SetFloat("SFXlowEQgain", _sfxPitchBoost);
            _masterMixer.SetFloat("SFXpitch", _sfxPitchShift);
        }

        /// <summary>
        /// Plays an ambient sound. Play space noises when there's no air, and station noises when there's air.
        /// </summary>
        private void PlayAmbience()
        {
            Debug.Log("Playing ambience.");
            if (_air == 0f)
            {
                _nowPlaying = PickSound(_spaceNoises);
            }
            else
            {
                _nowPlaying = PickSound(_environmentNoises);
            }
            _noisePlayer.clip = _nowPlaying;
            _noisePlayer.Play();
            StartCoroutine(AmbientNoiseTimer());
        }

        /// <summary>
        /// Grabs a clip from the supplied array.
        /// </summary>
        private AudioClip PickSound(AudioClip[] availableSounds)
        {
            //Pick a clip from the supplied array and return it
            AudioClip currentClip = availableSounds[Random.Range(0, availableSounds.Length)];
            return currentClip;
        }

        /// <summary>
        /// Waits a certain amount of time until playing ambience.
        /// </summary>
        IEnumerator AmbientNoiseTimer()
        {
            yield return new WaitForSeconds(_ambientNoiseFrequency);
            // If we generate a number bigger than our chance, then the attempt fails, and we try again in x minutes.
            if (Random.Range(1, 101) < _ambientNoiseChance)
            {
                PlayAmbience();
            }
            else
            {
                StartCoroutine(AmbientNoiseTimer());
            }

        }

        private void SetVolumeOfAll(float volume)
        {
            _airPlayer.volume = volume;
            _electricalPlayer.volume = volume;
            _lightWindPlayer.volume = volume;
            _heavyWindPlayer.volume = volume;
        }
    }
}
