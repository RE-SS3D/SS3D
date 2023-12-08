using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace System.Electricity
{
    public class BasicBattery : BasicElectricDevice, IPowerStorage
    {
        [SerializeField]
        private float _maxCapacity = 1000;

        [SerializeField, ReadOnly]
        private float _storedPower = 0;

        [SerializeField, ReadOnly]
        private float _maxPowerRate = 5f;
        public float StoredPower { get => _storedPower; set => _storedPower = value >= 0 ? MathF.Min(MaxCapacity, value) : MathF.Max(0f, value); }

        public float MaxCapacity => _maxCapacity;

        public float RemainingCapacity => _maxCapacity - _storedPower;

        public float MaxPowerRate => _maxPowerRate;

        public float MaxRemovablePower => _maxPowerRate > _storedPower ? _storedPower : _maxPowerRate;

        public float AddPower(float amount)
        {
            if (amount <= 0) return 0;

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

        public float RemovePower(float amount)
        {
            if (amount <= 0) return 0;

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
    }
}
