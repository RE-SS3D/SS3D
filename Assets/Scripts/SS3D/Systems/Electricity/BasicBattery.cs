using FishNet.Object.Synchronizing;
using SS3D.Systems.Tile.Connections;
using System;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// A basic implementation of the IPowerStorage interface.
    /// </summary>
    public class BasicBattery : BasicElectricDevice, IPowerStorage
    {
        [SerializeField][SyncVar]
        private float _maxCapacityKwh = 100f;

        [SerializeField][SyncVar]
        private float _storedEnergyKwh;

        [SerializeField][SyncVar]
        private float _maxDischargeRateKw = 50f;

        [SerializeField][SyncVar]
        private float _maxChargeRateKw = 50f;

        [SyncVar(OnChange = nameof(HandleSyncEnabled))]
        protected bool _isOn = true;

        public float StoredEnergyKwh
        {
            get => _storedEnergyKwh;
            set => _storedEnergyKwh = Mathf.Clamp(value, 0f, MaxCapacityKwh);
        }

        public float MaxCapacityKwh => _maxCapacityKwh;

        public float RemainingCapacityKwh => Mathf.Max(0f, _maxCapacityKwh - _storedEnergyKwh);

        public float MaxDischargeRateKw => _maxDischargeRateKw;

        public float MaxChargeRateKw
        {
            get => _maxChargeRateKw;
            set => _maxChargeRateKw = Mathf.Max(0f, value);
        }

        public float MaxDeliverableKw(float tickSeconds) =>
            PowerStorageMath.MaxDeliverableKw(_storedEnergyKwh, _maxDischargeRateKw, tickSeconds, _isOn);

        public bool IsOn { get => _isOn; set => _isOn = value; }

        public void Init(float maxDischargeRateKw, float maxCapacityKwh, float storedEnergyKwh, float maxChargeRateKw = -1f)
        {
            _maxDischargeRateKw = Mathf.Max(0f, maxDischargeRateKw);
            _maxCapacityKwh = Mathf.Max(0f, maxCapacityKwh);
            _maxChargeRateKw = maxChargeRateKw < 0f ? _maxDischargeRateKw : Mathf.Max(0f, maxChargeRateKw);
            StoredEnergyKwh = storedEnergyKwh;
        }

        public float AddPowerKw(float requestedKw, float tickSeconds) =>
            PowerStorageMath.AddPowerKw(
                ref _storedEnergyKwh,
                _maxCapacityKwh,
                _maxChargeRateKw,
                requestedKw,
                tickSeconds,
                _isOn);

        public float RemovePowerKw(float requestedKw, float tickSeconds) =>
            PowerStorageMath.RemovePowerKw(
                ref _storedEnergyKwh,
                _maxDischargeRateKw,
                requestedKw,
                tickSeconds,
                _isOn);

        protected virtual void HandleSyncEnabled(bool oldValue, bool newValue, bool asServer) { }
    }
}
