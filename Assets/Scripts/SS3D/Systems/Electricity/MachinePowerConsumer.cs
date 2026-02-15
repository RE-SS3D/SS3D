using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile.Connections;
using UnityEngine;
namespace System.Electricity
{
    public class MachinePowerConsumer : BasicElectricDevice, IPowerConsumer
    {
        [SerializeField]
        private float _powerConsumptionIdle = 1f;
        [SerializeField]
        private float _powerConsumptionInUse = 1f;

        public bool isIdle = true;
        private bool _machineUsedOnce;

        [SyncVar(OnChange = nameof(SyncPowerStatus))]
        private PowerStatus _powerStatus;
        public float PowerNeeded 
        {
            get
            {
                return isIdle ? _powerConsumptionIdle : _powerConsumptionInUse;
            }
        }
        
        public event EventHandler<PowerStatus> OnPowerStatusUpdated;
        public PowerStatus PowerStatus { get => _powerStatus; set => _powerStatus = value; }

        private void SyncPowerStatus(PowerStatus oldValue, PowerStatus newValue, bool asServer)
        {
            OnPowerStatusUpdated?.Invoke(this, newValue);
        }

        public void UseMachineOnce()
        {
            isIdle = false;
            _machineUsedOnce = true;
            SubSystems.Get<ElectricitySubSystem>().OnTick += HandleMachineWasUsed;
        }
        
        private void HandleMachineWasUsed()
        {
            _machineUsedOnce = false;
            SubSystems.Get<ElectricitySubSystem>().OnTick -= HandleMachineWasUsed;
        }
    }
}