using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
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
    }
}
