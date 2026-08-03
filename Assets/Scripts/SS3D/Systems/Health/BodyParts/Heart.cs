using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class Heart : BodyPart
    {
        // Number of beat per minutes
        private float _beatFrequency = 60f;

        public event EventHandler OnPulse;

        public float SecondsBetweenBeats => _beatFrequency > 0 ? 60f / _beatFrequency : float.MaxValue;

        private float _timer = 0f;

        /// <summary>
        /// Cardiac-output scalar: 1 at the resting 60 BPM, proportional to heart rate, and 0 when the heart is stopped
        /// or destroyed. The <see cref="CirculatoryController"/> multiplies the oxygen flow cap by this, so heart rate
        /// governs how much oxygen the circulation can move per second.
        /// </summary>
        public float CardiacOutputFactor => _beatFrequency > 0f ? _beatFrequency / 60f : 0f;

        public override void OnStartServer()
        {
            base.OnStartServer();
            StartCoroutine(DelayInit());
        }

        /// <summary>
        /// Necessary to prevent issue with body part not getting attached ...
        /// TODO : Implement a proper pipeline of initialisation.
        /// </summary>
        private IEnumerator DelayInit()
        {
            yield return null;
            yield return null;

            if (HealthController == null)
            {
                HealthController = GetComponentInParent<HealthController>();
            }
        }

        /// <summary>
        /// Advance the heartbeat clock by deltaTime and fire <see cref="OnPulse"/> once a full beat interval has
        /// elapsed. Driven by <see cref="CirculatoryController.MetabolicTick"/> so the whole entity runs off one server
        /// tick. OnPulse still drives bleeding (the CirculatoryController subscribes to it); the heart no longer
        /// delivers oxygen itself.
        /// </summary>
        [Server]
        public void BeatTick(float deltaTime)
        {
            _timer += deltaTime;

            if (_timer < SecondsBetweenBeats)
            {
                return;
            }

            _timer = 0f;
            OnPulse?.Invoke(this, EventArgs.Empty);
        }

        [Server]
        protected override void AfterSpawningCopiedBodyPart()
        {
        }

        [Server]
        protected override void BeforeDestroyingBodyPart()
        {
        }

        [Server]
        protected override void AddInitialLayers()
        {
            TryAddBodyLayer(new MuscleLayer(this));
            TryAddBodyLayer(new CirculatoryLayer(this, 3f));
            TryAddBodyLayer(new NerveLayer(this));
            TryAddBodyLayer(new OrganLayer(this));
        }

        /// <summary>
        /// Simply set the heart frequency, the default is 60 BPM. A frequency of 0 stops the heart:
        /// <see cref="CardiacOutputFactor"/> becomes 0 (no oxygen is moved, so the body suffocates) and
        /// <see cref="SecondsBetweenBeats"/> becomes infinite (no pulse, so no bleeding).
        /// </summary>
        [Server]
        public void SetBeatFrequency(float frequency)
        {
            _beatFrequency = frequency;
        }
    }
}