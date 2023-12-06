using SS3D.Core;
using SS3D.Data.Enums;
using SS3D.Systems.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace System.Electricity
{
    public class FuelPowerGenerator : BasicElectricDevice, IPowerProducer
    {
        [SerializeField]
        private float _powerProduction = 10f;
        public float PowerProduction => _powerProduction;

        protected override void OnStart()
        {
            base.OnStart();
            GetComponent<FuelPowerGeneratorInteractionTarget>().OnGeneratorToggle += HandleGeneratorToggle;
        }

        private void HandleGeneratorToggle(bool enabled)
        {
            HandleSound(enabled);
            HandleLights(enabled);
            HandlePowerGenerated(enabled);
        }

        private void HandleSound(bool enabled)
        {
            if (!enabled)
            {
                Subsystems.Get<AudioSystem>().StopAudioSource(NetworkObject);
            }
            else
            {
                Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.music, SoundsIds.FuelPowerGenerator, Position, NetworkObject, 0.7f, 1, 1, 10);
            }
        }

        private void HandleLights(bool enabled)
        {
            
        }

        private void HandlePowerGenerated(bool enabled)
        {
            if (!enabled)
            {
                _powerProduction = 0f;
            }
            else
            {
                _powerProduction = 10f;
            }
        }
    }
}
