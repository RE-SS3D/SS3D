using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class CirculatoryController : NetworkActor
    {
        [SerializeField]
        private Heart _heart;

        [SerializeField]
        private SubstanceContainer _container;

        [SerializeField]
        private HealthController _healthController;

        private Substance _blood;
        private Substance _oxygen;

        public SubstanceContainer Container => _container;

        /// <summary>
        /// The volume of blood in mLs the substance container can contain as a maximum. the 1 plus MaxOxygenToBloodVolumeRatio factor
        /// is there to "leave place" to oxygen in the container, which takes up a significant amount 
        /// (up to MaxOxygenToBloodVolumeRatio times 100 % of the blood volume).
        /// </summary>
        public float MaxBloodVolume =>  _healthController.BodyPartsVolume * HealthConstants.BloodVolumeToHumanVolumeRatio /
            (1+HealthConstants.MaxOxygenToBloodVolumeRatio);

        /// <summary>
        /// The maximum amount of blood in mmols _container can handle.
        /// </summary>
        public float MaxBloodQuantity => MaxBloodVolume / _blood.MillilitersPerMilliMoles;

        /// <summary>
        /// The maximum volume in mLs of oxygen _container can handle.
        /// </summary>
        private float MaxOxygenFullBloodVolume => HealthConstants.MaxOxygenToBloodVolumeRatio * MaxBloodVolume;


        /// <summary>
        /// Maximum volume of oxygen in mLs that can fit in _container, given the present amount of blood in it.
        /// </summary>
        public float MaxOxygenVolume => HealthConstants.MaxOxygenToBloodVolumeRatio * _container.GetSubstanceVolume(_blood);


        /// <summary>
        /// Maximum amount of oxygen in mmols that can fit in _container, given the present amount of blood in it.
        /// </summary>
        public float MaxOxygenQuantity => MaxOxygenVolume / _oxygen.MillilitersPerMilliMoles;

        /// <summary>
        /// The maximum amount of oxygen in mmols _container can handle.
        /// </summary>
        private float MaxOxygenFullBloodQuantity => MaxOxygenFullBloodVolume / _oxygen.MillilitersPerMilliMoles;

        /// <summary>
        /// Max total volume _container can have. The tolerance factor allows for a margin, but trouble can occurs if 
        /// the volume of the container is above MaxOxygenVolume + MaxBloodVolume.
        /// </summary>
        public float MaxTotalVolume => (MaxOxygenFullBloodVolume + MaxBloodVolume) * HealthConstants.HighBloodVolumeToleranceFactor;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SubstancesSubSystem registry = SubSystems.Get<SubstancesSubSystem>();
            _blood = registry.FromType(SubstanceType.Blood);
            _oxygen = registry.FromType(SubstanceType.Oxygen);
            StartCoroutine(Init());
        }

        /// <summary>
        /// Has to wait a bit to set up all body parts, otherwise fail to get bodypart attached to heart.
        /// </summary>
        /// <returns></returns>
        [Server]
        private IEnumerator Init()
        {
            yield return null;
            yield return null;

            UpdateVolume();
            AddInitialSubstance();
        }

        /// <summary>
        /// Should be called when a body part is disconnected from heart, as the total volume of the circulatory container should
        /// become smaller (or bigger if a body part is added).
        /// TODO : should be called whenever a bodypart is destroyed or detached.
        /// </summary>
        [Server]
        private void UpdateVolume()
        {
            _container.ChangeVolume(MaxTotalVolume);
        }

        /// <summary>
        /// Return the amount of oxygen the circulatory system can send to organs.
        /// If the blood quantity is above a given treshold, all oxygen in the circulatory container is available.
        /// If blood gets below, it starts diminishing the availability of oxygen despite the circulatory system containing enough.
        /// This is to mimick the lack of blood making oxygen transport difficult and potentially leading to organ suffocation.
        /// </summary>
        [Server]
        public double AvailableOxygen()
        {
            float bloodVolume = _container.GetSubstanceVolume(_blood);

            float healthyBloodVolume = HealthConstants.HealthyBloodVolumeRatio * MaxBloodVolume;

            float oxygenQuantity = _container.GetSubstanceQuantity(_oxygen);

            if(oxygenQuantity > MaxOxygenQuantity)
            {
                _container.RemoveSubstance(_oxygen, oxygenQuantity - MaxOxygenQuantity);
            }

            return bloodVolume > healthyBloodVolume ? oxygenQuantity : (bloodVolume / healthyBloodVolume) * oxygenQuantity;
        }

        /// <summary>
        /// Compute the need in oxygen of every body part in the provided list.
        /// </summary>
        [Server]
        public float[] ComputeIndividualNeeds(ReadOnlyCollection<BodyPart> parts)
        {
            float[] oxygenNeededForEachpart = new float[parts.Count];
            int i = 0;
            foreach (BodyPart bodyPart in parts)
            {
                bodyPart.TryGetBodyLayer(out CirculatoryLayer circulatory);
                oxygenNeededForEachpart[i] = (float)circulatory.OxygenNeeded;
                i++;
            }
            return oxygenNeededForEachpart;
        }

        /// <summary>
        /// Simply add oxygen and blood in the maximum allowed amount.
        /// </summary>
        /// <returns></returns>
        public void AddInitialSubstance()
        {
            _container.AddSubstance(_blood, MaxBloodQuantity);
            _container.AddSubstance(_oxygen, MaxOxygenFullBloodQuantity);
        }
    }
}
