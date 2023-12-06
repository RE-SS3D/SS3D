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
        public float StoredPower { get => _storedPower; set => _storedPower = value >= 0 ? MathF.Min(MaxCapacity, value) : MathF.Max(0f, value); }

        public float MaxCapacity => _maxCapacity;

        public float RemainingCapacity => _maxCapacity - _storedPower;
    }
}
