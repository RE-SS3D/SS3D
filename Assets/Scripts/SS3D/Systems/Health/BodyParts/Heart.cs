﻿using FishNet.Object;
using SS3D.Core;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Health
{
    public class Heart : BodyPart
    {

        // Number of beat per minutes
        private float _beatFrequency = 60f;

        public event EventHandler OnPulse;

        public float SecondsBetweenBeats => _beatFrequency > 0 ? 60f / _beatFrequency : float.MaxValue;

        private float _timer = 0f;

        private float _oxygenSumNeeded; 

        private List<BodyPart> _connectedToHeart;

        public float OxygenSumNeeded => _oxygenSumNeeded;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _connectedToHeart = GetAllBodyPartAttachedToHeart();
            OnPulse += HandleHeartPulse;
            HealthController.OnBodyPartRemoved += HandleBodyPartRemoved;
        }

        [Server]
        public void HandleHeartPulse(object sender, EventArgs args)
        {          
            SendOxygen();
            Bleed();
        }

        void Update()
        {
            _timer += Time.deltaTime;

            if (_timer > SecondsBetweenBeats)
            {
                _timer = 0f;
                OnPulse?.Invoke(this, EventArgs.Empty);
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

        [Server]
        protected override void AddInitialLayers()
        {
            TryAddBodyLayer(new MuscleLayer(this));
            TryAddBodyLayer(new CirculatoryLayer(this, 3f));
            TryAddBodyLayer(new NerveLayer(this));
            TryAddBodyLayer(new OrganLayer(this));
        }

        /// <summary>
        /// Send oxygen to all connected circulatory layers to heart.
        /// Send a bit more than necessary when oxygen is available to restore oxygen reserves in each circulatory layers.
        /// </summary>
        [Server]
        private void SendOxygen()
        {
            double availableOxygen =  HealthController.Circulatory.AvailableOxygen();

            float[] oxygenNeededForEachpart = 
                HealthController.Circulatory.ComputeIndividualNeeds(_connectedToHeart.AsReadOnly());

            float sumNeeded = oxygenNeededForEachpart.Sum();
            int i = 0;

            SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
            Substance oxygen = registry.FromType(SubstanceType.Oxygen);

            foreach (BodyPart part in _connectedToHeart)
            {
                part.TryGetBodyLayer(out CirculatoryLayer veins);
                double proportionAvailable = 0;
                if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
                {
                    proportionAvailable = oxygenNeededForEachpart[i] / sumNeeded;
                    veins.ReceiveOxygen(HealthConstants.SafeOxygenFactor * proportionAvailable * availableOxygen);
                }
                else
                {
                    proportionAvailable = oxygenNeededForEachpart[i] / sumNeeded;
                    veins.ReceiveOxygen(proportionAvailable * availableOxygen);
                }
                i++;
            }

            if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
            {
                HealthController.Circulatory.Container.RemoveSubstance(oxygen, HealthConstants.SafeOxygenFactor * sumNeeded);
            }
            else
            {
                HealthController.Circulatory.Container.RemoveSubstance(oxygen, (float)availableOxygen);
            }
        }

        [Server]
        public void HandleBodyPartRemoved(object sender, BodyPart part)
        {
            _connectedToHeart.Remove(part);
        }

        private void Bleed()
        {
            foreach (BodyPart part in _connectedToHeart)
            {
                part.TryGetBodyLayer(out CirculatoryLayer veins);
                veins.Bleed();
            }
        }

        /// <summary>
        /// Get a list of all body part attached to heart, including all internal organs.
        /// A body part is considered attached to heart if it's either the external body part of heart, 
        /// a child of the latter or an internal body part of any, with the condition that they need to have a circulatory layer.
        /// Fixing a living foot on a wooden leg won't prevent it from dying.
        /// </summary>
        [Server]
        private List<BodyPart> GetAllBodyPartAttachedToHeart()
        {
            List<BodyPart> connectedToHeart = new List<BodyPart>();

            if (IsInsideBodyPart)
            {
                BodyPart heartContainer = ExternalBodyPart;
                GetAllBodyPartAttachedToHeartRecursion(connectedToHeart, heartContainer);
            }
            return connectedToHeart;
        }

        /// <summary>
        /// Helper method for GetAllBodyPartAttachedToHeart().
        /// </summary>
        [Server]
        private void GetAllBodyPartAttachedToHeartRecursion(List<BodyPart> connectedToHeart, BodyPart current)
        {
            if (current.ContainsLayer(BodyLayerType.Circulatory))
            {
                connectedToHeart.Add(current);
            }

            if (current.HasInternalBodyPart)
            {
                foreach (var part in current.InternalBodyParts)
                {
                    connectedToHeart.Add(part);
                }
            }

            foreach (BodyPart bodyPart in current.ChildBodyParts.Where(x => x.ContainsLayer(BodyLayerType.Circulatory)))
            {
                GetAllBodyPartAttachedToHeartRecursion(connectedToHeart, bodyPart);
            }
        }

        [Server]
        public void SetBeatFrequency(float frequency)
        {
            _beatFrequency = frequency;
        }
    }
}