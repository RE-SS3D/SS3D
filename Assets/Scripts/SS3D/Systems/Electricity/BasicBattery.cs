using FishNet.Object.Synchronizing;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace System.Electricity
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

        public float MaxDeliverableKw(float tickSeconds)
        {
            if (!_isOn || _storedEnergyKwh <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(_maxDischargeRateKw, ElectricityUnits.KwhToKw(_storedEnergyKwh, tickSeconds));
        }

        public bool IsOn { get => _isOn; set => _isOn = value; }

        public void Init(float maxDischargeRateKw, float maxCapacityKwh, float storedEnergyKwh, float maxChargeRateKw = -1f)
        {
            _maxDischargeRateKw = Mathf.Max(0f, maxDischargeRateKw);
            _maxCapacityKwh = Mathf.Max(0f, maxCapacityKwh);
            _maxChargeRateKw = maxChargeRateKw < 0f ? _maxDischargeRateKw : Mathf.Max(0f, maxChargeRateKw);
            StoredEnergyKwh = storedEnergyKwh;
        }

        public float AddPowerKw(float requestedKw, float tickSeconds)
        {
            if (requestedKw <= 0f || !_isOn || RemainingCapacityKwh <= 0f || _maxChargeRateKw <= 0f)
            {
                return 0f;
            }

            float absorbedKw = Mathf.Min(requestedKw, _maxChargeRateKw);
            float energyToAdd = ElectricityUnits.KwToKwh(absorbedKw, tickSeconds);
            float addedEnergy = Mathf.Min(RemainingCapacityKwh, energyToAdd);
            _storedEnergyKwh += addedEnergy;
            return ElectricityUnits.KwhToKw(addedEnergy, tickSeconds);
        }

        public float RemovePowerKw(float requestedKw, float tickSeconds)
        {
            if (requestedKw <= 0f || !_isOn || _storedEnergyKwh <= 0f)
            {
                return 0f;
            }

            float deliverableKw = MaxDeliverableKw(tickSeconds);
            float deliveredKw = Mathf.Min(requestedKw, deliverableKw);
            float removedEnergy = ElectricityUnits.KwToKwh(deliveredKw, tickSeconds);
            _storedEnergyKwh -= removedEnergy;
            return deliveredKw;
        }

        protected virtual void HandleSyncEnabled(bool oldValue, bool newValue, bool asServer) { }
    }
}
