using FishNet.Object;
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

            _connectedToHeart = GetAllBodyPartAttachedToHeart();

            OnPulse += HandleHeartPulse;

            //TODO : temporary fix for heart when it's not attached to a health controller. Should eventually prevent working
            // when heart is detached from head.
            if (HealthController != null)
            {
                HealthController.OnBodyPartRemoved += HandleBodyPartRemoved;
            } 
        }

        /// <summary>
        /// Simple handler for pulse event.
        /// </summary>
        [Server]
        public void HandleHeartPulse(object sender, EventArgs args)
        {
            //TODO : temporary fix for heart when it's not attached to a health controller. Should eventually prevent working
            // when heart is detached from head.
            if (HealthController == null) return;

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

            SubstancesSubSystem registry = SubSystems.Get<SubstancesSubSystem>();
            Substance oxygen = registry.FromType(SubstanceType.Oxygen);

            foreach (BodyPart part in _connectedToHeart)
            {
                part.TryGetBodyLayer(out CirculatoryLayer veins);
                double proportionAvailable = 0;
                if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
                {
                    // Send a bit more if it can afford it.
                    proportionAvailable = oxygenNeededForEachpart[i] / sumNeeded;
                    veins.ReceiveOxygen(HealthConstants.SafeOxygenFactor * proportionAvailable * availableOxygen);
                }
                else
                {
                    // Send just what it needs to survive (or less).
                    proportionAvailable = oxygenNeededForEachpart[i] / sumNeeded;
                    veins.ReceiveOxygen(proportionAvailable * availableOxygen);
                }
                i++;
            }

            // Remove all oxygen sent to body parts from the substance container.
            if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
            {
                HealthController.Circulatory.Container.RemoveSubstance(oxygen, HealthConstants.SafeOxygenFactor * sumNeeded);
            }
            else
            {
                HealthController.Circulatory.Container.RemoveSubstance(oxygen, (float)availableOxygen);
            }
        }

        /// <summary>
        /// Simply remove a body part connected to heart
        /// </summary>
        [Server]
        public void HandleBodyPartRemoved(object sender, BodyPart part)
        {
            _connectedToHeart.Remove(part);
        }

        /// <summary>
        /// Make all circulatory layer connected to heart bleed.
        /// </summary>
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

        /// <summary>
        /// Simply set the heart frequency, the default is 60 BPM.
        /// </summary>
        [Server]
        public void SetBeatFrequency(float frequency)
        {
            _beatFrequency = frequency;
        }
    }
}
