using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Logging;
using SS3D.Systems.Audio;
using SS3D.Systems.Tile.Connections;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Electricity
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
        public float PowerProduction => _enabled ? _onPowerProduction : 0f;

        [SyncVar(OnChange = nameof(SyncGeneratorToggle))]
        private bool _enabled = false; // If the generator is working.
        private float _onPowerProduction = 10f;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _onPowerProduction = _powerProduction;
            // OnToggle is raised on the server only; subscribing in OnStartClient never ran on a dedicated server.
            GetComponent<GenericToggleInteractionTarget>().OnToggle += HandleGeneratorToggle;
        }

        public override void OnStopServer()
        {
            if (TryGetComponent(out GenericToggleInteractionTarget toggle))
                toggle.OnToggle -= HandleGeneratorToggle;

            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _onPowerProduction = _powerProduction;
            HandlePowerGenerated(false);
        }

        [Server]
        private void HandleGeneratorToggle(bool isEnabled)
        {
            _enabled = isEnabled;
            _powerProduction = isEnabled ? _onPowerProduction : 0f;
        }

        private void SyncGeneratorToggle(bool oldValue, bool newValue, bool asServer)
        {
            if (asServer)
            {
                // Playback is server-owned: the audio subsystem's play/stop entry points are
                // [Server], so a client calling them only logs "server is not active" and
                // returns. Broadcast from here instead.
                RpcSyncRunningSound(newValue);
                return;
            }

            HandleLights(newValue);
            HandlePowerGenerated(newValue);
            GetComponent<MachineVibrate>().Enable = newValue;
        }

        /// <summary>
        /// Buffered so a client that joins, or comes back into AOI, while the generator is
        /// already running starts the loop. The audio subsystem's play call is one-shot and
        /// only reaches observers that existed at that moment.
        /// </summary>
        [ObserversRpc(BufferLast = true)]
        private void RpcSyncRunningSound(bool isEnabled)
        {
            AudioSubSystem audio = SubSystems.Get<AudioSubSystem>();
            if (audio == null)
            {
                return;
            }

            if (isEnabled)
            {
                audio.PlayAudioSourceLocal(AudioType.Music, Sounds.FuelPowerGenerator, Position, NetworkObject,
                    true, 0.7f, 1, 1, 10);
            }
            else
            {
                audio.StopAudioSourceLocal(NetworkObject);
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
                Log.Error(typeof(FuelPowerGenerator), "Blend shape {blendShapeName} not found.", Logs.Generic, OnBlendShapeName);
            }

            if (outputBlendShapeIndex != -1)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(outputBlendShapeIndex, isEnabled ? 100 : 0);
            }
            else
            {
                Log.Error(typeof(FuelPowerGenerator), "Blend shape {blendShapeName} not found.", Logs.Generic, OutputBlendShapeName);
            }
        }

        private void HandlePowerGenerated(bool isEnabled)
        {
            _powerProduction = isEnabled ? _onPowerProduction : 0f; 
        }
    }
}
