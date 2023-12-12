using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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

        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        private const string OnBlendShapeName = "On";

        private const string OutputBlendShapeName = "Output";

        private const string LowFuel = "LowFuel";
        public float PowerProduction => _powerProduction;

        private Quaternion _initialRotation; // Rotation of the generator at rest.

        private Vector3 _directionOfShake; // In which direction the generator shake.

        [SerializeField]
        private float _amplitude = 1; // How much is the amplitude of the vibration.

        [SerializeField]
        private float _frequency = 35; // Vibrating speed.

        private float _elapsedTime = 0f; // Elapsed time for the vibrating stuff.

        [SyncVar(OnChange = nameof(SyncGeneratorToggle))]
        private bool _enabled = false; // If the generator is working.

        private float _onPowerProduction = 10f;


        public override void OnStartClient()
        {
            base.OnStartClient();
            GetComponent<GenericToggleInteractionTarget>().OnToggle += HandleGeneratorToggle;
            AddHandle(FixedUpdateEvent.AddListener(HandleFixedUpdate));
            _initialRotation = Rotation;
            _directionOfShake = Transform.right;
            _onPowerProduction = _powerProduction;
            HandlePowerGenerated(false);
        }

        private void HandleFixedUpdate(ref EventContext context, in FixedUpdateEvent updateEvent)
        {
            if (_enabled) Vibrate();
        }

        [Server]
        private void HandleGeneratorToggle(bool enabled)
        {
            _enabled = enabled;
        }

        private void SyncGeneratorToggle(bool oldValue, bool newValue, bool asServer)
        {
            if (asServer) return;
            if (newValue) _initialRotation = Rotation;

            HandleSound(newValue);
            HandleLights(newValue);
            HandlePowerGenerated(newValue);
            HandleResetVibration();
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

        private void HandleResetVibration()
        {
            Rotation = _initialRotation;
        }

        private void Vibrate()
        {
            _elapsedTime += Time.fixedDeltaTime;
            transform.rotation = _initialRotation * Quaternion.Euler(_directionOfShake * (-_amplitude + Mathf.PingPong(_frequency * _elapsedTime, 2f * _amplitude)));
        }
    }
}
