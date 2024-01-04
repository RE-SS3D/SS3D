using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// Script providing a basic implementation for IPowerConsumer.
    /// Can be used for things that need a constant amount of power at all time.
    /// </summary>
    public class BasicPowerConsumer : BasicElectricDevice, IPowerConsumer
    {
        [SerializeField]
        private float _powerConsumption = 1f;

        [SyncVar(OnChange = nameof(SyncPowerStatus))]
        private PowerStatus _powerStatus;
        public float PowerNeeded => _powerConsumption;
        public event EventHandler<PowerStatus> OnPowerStatusUpdated;
        public PowerStatus PowerStatus { get => _powerStatus; set => _powerStatus = value; }

        public void Init(float powerConsumption)
        {
            _powerConsumption = MathF.Max(powerConsumption, 0);
        }

        private void SyncPowerStatus(PowerStatus oldValue, PowerStatus newValue, bool asServer)
        {
            OnPowerStatusUpdated.Invoke(this, newValue);
        }
    }
}
