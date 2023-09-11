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

        public SubstanceContainer Container => _container;

        public override void OnStartServer()
        {
            base.OnStartServer();
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
        }

        /// <summary>
        /// Should be called when a body part is disconnected from heart, as the total volume of the circulatory container should
        /// become smaller (or bigger if a body part is added).
        /// TODO : should be called whenever a bodypart is destroyed or detached.
        /// </summary>
        [Server]
        private void UpdateVolume()
        {
            BodyPart[] allBodyPartsOnEntity = GetComponentsInChildren<BodyPart>();

            _container.ChangeVolume((float)allBodyPartsOnEntity.Sum(x => x.Volume) * HealthConstants.BloodVolumeToHumanVolumeRatio);
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
            SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
            Substance blood = registry.FromType(SubstanceType.Blood);

            Substance oxygen = registry.FromType(SubstanceType.Oxygen);

            float bloodVolume = _container.GetSubstanceVolume(blood);

            float healthyBloodVolume = (float)HealthConstants.HealthyBloodVolumeRatio * _container.Volume;

            double oxygenQuantity = _container.GetSubstanceQuantity(oxygen);

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
    }
}
