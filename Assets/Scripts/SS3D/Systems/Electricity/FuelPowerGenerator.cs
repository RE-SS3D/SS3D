using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace System.Electricity
{
    /// <summary>
    /// Script for the pacman generator, handling light and noise when turning it on and off. 
    /// </summary>
    public class FuelPowerGenerator : BasicElectricDevice, IPowerProducer
    {
        [SerializeField]
        private float _powerProduction = 10f;

        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        private const string OnBlendShapeName = "On";

        private const string OutputBlendShapeName = "Output";

        private const string LowFuel = "LowFuel";
        public float PowerProduction => _powerProduction;

        [SyncVar(OnChange = nameof(SyncGeneratorToggle))]
        private bool _enabled = false; // If the generator is working.

        private float _onPowerProduction = 10f;


        public override void OnStartClient()
        {
            base.OnStartClient();
            GetComponent<GenericToggleInteractionTarget>().OnToggle += HandleGeneratorToggle;
            _onPowerProduction = _powerProduction;
            HandlePowerGenerated(false);
        }

        [Server]
        private void HandleGeneratorToggle(bool enabled)
        {
            _enabled = enabled;
        }

        private void SyncGeneratorToggle(bool oldValue, bool newValue, bool asServer)
        {
            if (asServer) return;

            HandleSound(newValue);
            HandleLights(newValue);
            HandlePowerGenerated(newValue);
            GetComponent<MachineVibrate>().Enable = newValue;
        }

        private void HandleSound(bool enabled)
        {
            if (!enabled)
            {
                Subsystems.Get<AudioSystem>().StopAudioSource(NetworkObject);
            }
            else
            {
                Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.Music, Sounds.FuelPowerGenerator, Position, NetworkObject,
                    true, 0.7f, 1, 1, 10);
            }
        }

        private void HandleLights(bool enabled)
        {
            int OnblendShapeIndex = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(OnBlendShapeName);
            int OutputBlendShapeIndex = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(OutputBlendShapeName);

            if (OnblendShapeIndex != -1)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(OnblendShapeIndex, enabled ? 100 : 0);
            }
            else
            {
                Debug.LogError("Blend shape " + OnBlendShapeName + " not found.");
            }

            if (OutputBlendShapeIndex != -1)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(OutputBlendShapeIndex, enabled ? 100 : 0);
            }
            else
            {
                Debug.LogError("Blend shape " + OnBlendShapeName + " not found.");
            }
        }

        private void HandlePowerGenerated(bool enabled)
        {
            _powerProduction = enabled ? _onPowerProduction : 0f; 
        }
    }
}
