using FishNet.Object;
using SS3D.Core;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Health
{
    public class Lungs : BodyPart
    {
        public enum BreathingState
        {
            Nice = 0,
            Difficult = 1,
            Suffocating = 2,
        }

        public event EventHandler OnBreath;

        private BreathingState _breathing;

        // Number of inspiration and expiration per minutes
        private float _breathFrequency = 60f;

        private float _timer;

        // TODO : remove this and replace with oxygen taken from atmos when possible
        [FormerlySerializedAs("OxygenConstantIntake")]
        [SerializeField]
        private float _oxygenConstantIntake = 0.4f;

        public float SecondsBetweenBreaths => _breathFrequency > 0 ? 60f / _breathFrequency : float.MaxValue;

        public override void OnStartServer()
        {
            base.OnStartServer();
            StartCoroutine(DelayInit());
        }

        [Server]
        protected override void AddInitialLayers()
        {
            TryAddBodyLayer(new MuscleLayer(this));
            TryAddBodyLayer(new CirculatoryLayer(this, 3f));
            TryAddBodyLayer(new NerveLayer(this));
            TryAddBodyLayer(new OrganLayer(this));
        }

        protected void Update()
        {
            if (!IsServer)
            {
                return;
            }

            _timer += Time.deltaTime;

            if (_timer > SecondsBetweenBreaths)
            {
                _timer = 0f;
                Breath();
            }
        }

        [Server]
        protected override void AfterSpawningCopiedBodyPart()
        {
        }

        [Server]
        protected override void BeforeDestroyingBodyPart()
        {
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
        /// Take some amount of gas from atmos and inject it in blood.
        /// TODO : Actually take gas from atmos, for now, constant intake of oxygen.
        /// </summary>
        [Server]
        private void Breath()
        {
            OnBreath?.Invoke(this, EventArgs.Empty);

            // TODO : temporary fix for lungs when they are not attached to a health controller. Should eventually prevent breathing
            // when lungs are detached from head.
            if (HealthController == null)
            {
                return;
            }

            SubstancesSubSystem registry = Subsystems.Get<SubstancesSubSystem>();
            Substance oxygen = registry.FromType(SubstanceType.Oxygen);
            if (HealthController.Circulatory.Container.GetSubstanceQuantity(oxygen) > HealthController.Circulatory.MaxOxygenQuantity)
            {
                return;
            }

            HealthController.Circulatory.Container.AddSubstance(oxygen, _oxygenConstantIntake);
            SetBreathingState();
        }

        /// <summary>
        /// Breathing state could be useful to set stuff like breathing noises. This set it up depending on the amount of
        /// available and needed oxygen. Breathing becomes more difficult as oxygen amount becomes uncomfortable.
        /// </summary>
        [Server]
        private void SetBreathingState()
        {
            float availableOxygen = (float)HealthController.Circulatory.AvailableOxygen();
            float sumNeeded = HealthController.Circulatory.ComputeIndividualNeeds(HealthController.BodyPartsOnEntity).Sum();

            if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
            {
                _breathing = BreathingState.Nice;
            }
            else if (availableOxygen > sumNeeded)
            {
                _breathing = BreathingState.Difficult;
            }
            else
            {
                _breathing = BreathingState.Suffocating;
            }
        }
    }
}
