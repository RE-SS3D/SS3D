using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace System.Electricity
{
    public class MachinePowerConsumer : BasicElectricDevice, IPowerConsumer
    {
        public event EventHandler<PowerStatus> OnPowerStatusUpdated;

        [SerializeField]
        private float _powerConsumptionIdle = 1f;

        [SerializeField]
        private float _powerConsumptionInUse = 1f;

        private bool _machineUsedOnce;

        [SyncVar(OnChange = nameof(SyncPowerStatus))]
        private PowerStatus _powerStatus;

        public bool IsIdle { get; set; }

        public float PowerNeeded => IsIdle ? _powerConsumptionIdle : _powerConsumptionInUse;

        public PowerStatus PowerStatus { get => _powerStatus; set => _powerStatus = value; }

        public void UseMachineOnce()
        {
            IsIdle = false;
            _machineUsedOnce = true;
            Subsystems.Get<ElectricitySubSystem>().OnTick += HandleMachineWasUsed;
        }

        private void SyncPowerStatus(PowerStatus oldValue, PowerStatus newValue, bool asServer)
        {
            OnPowerStatusUpdated?.Invoke(this, newValue);
        }

        private void HandleMachineWasUsed()
        {
            _machineUsedOnce = false;
            Subsystems.Get<ElectricitySubSystem>().OnTick -= HandleMachineWasUsed;
        }
    }
}
