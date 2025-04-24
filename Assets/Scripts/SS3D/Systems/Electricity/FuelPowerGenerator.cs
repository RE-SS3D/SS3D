using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Systems.Audio;
using SS3D.Systems.Tile.Connections;
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
        private void HandleGeneratorToggle(bool isEnabled)
        {
            _enabled = isEnabled;
        }

        private void SyncGeneratorToggle(bool oldValue, bool newValue, bool asServer)
        {
            if (asServer) return;

            HandleSound(newValue);
            HandleLights(newValue);
            HandlePowerGenerated(newValue);
            GetComponent<MachineVibrate>().Enable = newValue;
        }

        private void HandleSound(bool isEnabled)
        {
            if (!isEnabled)
            {
                SubSystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            }
            else
            {
                SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, Sounds.FuelPowerGenerator, Position, NetworkObject,
                    true, 0.7f, 1, 1, 10);
            }
        }

        private void HandleLights(bool isEnabled)
        {
            int onblendShapeIndex = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(OnBlendShapeName);
            int outputBlendShapeIndex = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(OutputBlendShapeName);

            if (onblendShapeIndex != -1)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(onblendShapeIndex, isEnabled ? 100 : 0);
            }
            else
            {
                Debug.LogError("Blend shape " + OnBlendShapeName + " not found.");
            }

            if (outputBlendShapeIndex != -1)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(outputBlendShapeIndex, isEnabled ? 100 : 0);
            }
            else
            {
                Debug.LogError("Blend shape " + OnBlendShapeName + " not found.");
            }
        }

        private void HandlePowerGenerated(bool isEnabled)
        {
            _powerProduction = isEnabled ? _onPowerProduction : 0f; 
        }
    }
}
