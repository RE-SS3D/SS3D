using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace System.Electricity
{
    /// <summary>
    /// A basic implementation of the IpowerStorage interface.
    /// </summary>
    public class BasicBattery : BasicElectricDevice, IPowerStorage
    {
        [SerializeField, SyncVar]
        private float _maxCapacity = 1000;

        [SerializeField, SyncVar]
        private float _storedPower = 0;

        [SerializeField, SyncVar]
        private float _maxPowerRate = 5f;

        [SyncVar(OnChange = nameof(SyncEnabled))]
        protected bool _isOn = true;

        /// <inheritdoc> </inheritdoc>
        public float StoredPower { get => _storedPower; set => _storedPower = value >= 0 ? MathF.Min(MaxCapacity, value) : MathF.Max(0f, value); }

        /// <inheritdoc> </inheritdoc>
        public float MaxCapacity => _maxCapacity;

        /// <inheritdoc> </inheritdoc>
        public float RemainingCapacity => _maxCapacity - _storedPower;

        /// <inheritdoc> </inheritdoc>
        public float MaxPowerRate => _maxPowerRate;

        /// <inheritdoc> </inheritdoc>
        public float MaxRemovablePower => _maxPowerRate > _storedPower ? _storedPower : _maxPowerRate;

        /// <inheritdoc> </inheritdoc>
        public bool IsOn => _isOn;


        /// <inheritdoc> </inheritdoc>
        public float AddPower(float amount)
        {
            if (amount <= 0 || !_isOn) return 0;

            if (_storedPower + amount > _maxCapacity)
            {
                _storedPower = _maxCapacity;
                return _maxCapacity - _storedPower;
            }
            else
            {
                _storedPower += amount;
                return amount;
            }
        }

        /// <inheritdoc> </inheritdoc>
        public float RemovePower(float amount)
        {
            if (amount <= 0 || !_isOn) return 0;

            if (!_isOn) return amount;

            if (_storedPower - amount < 0)
            {
                _storedPower = 0;
                return _storedPower;
            }
            else
            {
                _storedPower -= amount;
                return amount;
            }
        }

        protected virtual void SyncEnabled(bool oldValue, bool newValue, bool asServer) { }
    }
}
