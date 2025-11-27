using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using SS3D.Systems.Interactions;
using SS3D.Systems.Tile.Connections;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace System.Electricity
{
    /// <summary>
    /// Script for the pacman generator, handling light and noise when turning it on and off.
    /// </summary>
    public class FuelPowerGenerator : BasicElectricDevice, IPowerProducer, IInteractionPointProvider
    {
        private const string BlendShapeName = "On";

        private const string OutputBlendShapeName = "Output";

        private const string LowFuel = "LowFuel";

        [SerializeField]
        private float _powerProduction = 10f;

        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        [SyncVar(OnChange = nameof(SyncGeneratorToggle))]
        private bool _enabled; // If the generator is working.

        private float _onPowerProduction = 10f;

        [SerializeField]
        private Transform _toggleOn;

        [SerializeField]
        private Transform _toggleOff;

        public float PowerProduction => _powerProduction;

        public override void OnStartClient()
        {
            base.OnStartClient();
            GetComponent<GenericToggleInteractionTarget>().OnToggle += HandleGeneratorToggle;
            _onPowerProduction = _powerProduction;
            HandlePowerGenerated(false);
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point)
        {
            point = _enabled ? _toggleOff.position : _toggleOn.position;

            return true;
        }

        [Server]
        private void HandleGeneratorToggle(bool isEnabled)
        {
            _enabled = isEnabled;
        }

        private void SyncGeneratorToggle(bool oldValue, bool newValue, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            HandleSound(newValue);
            HandleLights(newValue);
            HandlePowerGenerated(newValue);
            GetComponent<MachineVibrate>().Enable = newValue;
        }

        private void HandleSound(bool isEnabled)
        {
            if (!isEnabled)
            {
                Subsystems.Get<AudioSubSystem>().StopAudioSource(NetworkObject);
            }
            else
            {
                Subsystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Music, Sounds.FuelPowerGenerator, Position, NetworkObject, true, 0.7f, 1, 1, 10);
            }
        }

        private void HandleLights(bool isEnabled)
        {
            int onblendShapeIndex = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            int outputBlendShapeIndex = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(OutputBlendShapeName);

            if (onblendShapeIndex != -1)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(onblendShapeIndex, isEnabled ? 100 : 0);
            }
            else
            {
                Debug.LogError("Blend shape " + BlendShapeName + " not found.");
            }

            if (outputBlendShapeIndex != -1)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(outputBlendShapeIndex, isEnabled ? 100 : 0);
            }
            else
            {
                Debug.LogError("Blend shape " + BlendShapeName + " not found.");
            }
        }

        private void HandlePowerGenerated(bool isEnabled)
        {
            _powerProduction = isEnabled ? _onPowerProduction : 0f;
        }
    }
}
