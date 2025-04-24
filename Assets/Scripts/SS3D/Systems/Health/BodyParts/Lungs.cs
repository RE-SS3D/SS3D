using FishNet.Object;
using SS3D.Core;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class Lungs : BodyPart
    {

        public enum BreathingState
        {
            Nice,
            Difficult,
            Suffocating
        }

        public BreathingState breathing;

        // Number of inspiration and expiration per minutes
        private float _breathFrequency = 60f;

        public event EventHandler OnBreath;

        private float _timer = 0f;

        // TODO : remove this and replace with oxygen taken from atmos when possible
        [SerializeField]
        private float OxygenConstantIntake = 0.4f;

        public float SecondsBetweenBreaths => _breathFrequency > 0 ? 60f / _breathFrequency : float.MaxValue;

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

            if(HealthController == null)
            {
                HealthController = GetComponentInParent<HealthController>();
            }
        }

        [Server]
        protected override void AddInitialLayers()
        {
            TryAddBodyLayer(new MuscleLayer(this));
            TryAddBodyLayer(new CirculatoryLayer(this, 3f));
            TryAddBodyLayer(new NerveLayer(this));
            TryAddBodyLayer(new OrganLayer(this));
        }

        void Update()
        {
            if (!IsServer) return;
            _timer += Time.deltaTime;

            if (_timer > SecondsBetweenBreaths)
            {
                _timer = 0f;
                Breath();
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

            //TODO : temporary fix for lungs when they are not attached to a health controller. Should eventually prevent breathing
            // when lungs are detached from head.
            if (HealthController == null) return;

            SubstancesSubSystem registry = SubSystems.Get<SubstancesSubSystem>();
            Substance oxygen = registry.FromType(SubstanceType.Oxygen);
            if (HealthController.Circulatory.Container.GetSubstanceQuantity(oxygen) > HealthController.Circulatory.MaxOxygenQuantity)
            {
                return;
            }
            else
            {
                HealthController.Circulatory.Container.AddSubstance(oxygen, OxygenConstantIntake);
            }

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
                breathing = BreathingState.Nice;
            }
            else if (availableOxygen > sumNeeded)
            {
                breathing = BreathingState.Difficult;
            }
            else
            {
                breathing = BreathingState.Suffocating;
            }
        }

        [Server]
        protected override void AfterSpawningCopiedBodyPart()
        {
            return;
        }

        [Server]
        protected override void BeforeDestroyingBodyPart()
        {
            return;
        }
    }
}
