using System.Collections.Generic;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Examine;
using SS3D.Systems.Tile.Connections;
using System;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Script providing a basic implementation for IPowerConsumer.
    /// Can be used for things that need a constant amount of power at all time.
    /// </summary>
    public class BasicPowerConsumer : BasicElectricDevice, IPowerConsumer, IExamineContentProvider
    {
        [SerializeField]
        private float _powerConsumption = 1f;

        [SerializeField]
        private PowerChannel _channel = PowerChannel.Equipment;

        [SyncVar(OnChange = nameof(SyncPowerStatus))]
        private PowerStatus _powerStatus;
        public float PowerNeeded => _powerConsumption;
        public PowerChannel Channel => _channel;
        public event EventHandler<PowerStatus> OnPowerStatusUpdated;
        public PowerStatus PowerStatus
        {
            get => _powerStatus;
            set
            {
                // SyncVar is server-authoritative — client assigns spam FishNet
                // "Cannot complete operation as server when server is not active".
                // Allow EditMode/offline (null or unspawned NetworkObject); block pure clients only.
                // NetworkObject first — IsServer NREs when _networkObjectCache is null.
                if (NetworkObject != null && NetworkObject.IsSpawned && !IsServer)
                {
                    return;
                }

                _powerStatus = value;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnPowerStatusUpdated?.Invoke(this, _powerStatus);
        }

        public void Init(float powerConsumption, PowerChannel channel = PowerChannel.Equipment)
        {
            _powerConsumption = MathF.Max(powerConsumption, 0);
            _channel = channel;
        }

        private void SyncPowerStatus(PowerStatus oldValue, PowerStatus newValue, bool asServer)
        {
            OnPowerStatusUpdated?.Invoke(this, newValue);
        }

        /// <summary>Powered or unpowered on examine, at any range (examine.md §7).</summary>
        public void AppendSections(IExaminable examinable, List<ExamineSection> sections) =>
            ExamineDeviceKeys.AppendPower(PowerStatus == PowerStatus.Powered, sections);
    }
}
