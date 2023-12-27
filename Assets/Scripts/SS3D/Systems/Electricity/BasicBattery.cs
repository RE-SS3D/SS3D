using FishNet.Object.Synchronizing;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// A basic implementation of the IpowerStorage interface.
    /// </summary>
    public class BasicBattery : BasicElectricDevice, IPowerStorage
    {
        [SerializeField][SyncVar]
        private float _maxCapacity = 1000;

        [SerializeField][SyncVar]
        private float _storedPower = 0;

        [SerializeField][SyncVar]
        private float _maxPowerRate = 5f;

        [SyncVar(OnChange = nameof(HandleSyncEnabled))]
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

            float addedAmount = Mathf.Min(RemainingCapacity, amount);
            _storedPower += addedAmount;
            return addedAmount;
        }

        /// <inheritdoc> </inheritdoc>
        public float RemovePower(float amount)
        {
            if (amount <= 0 || !_isOn) return 0;

            float removedAmount = Mathf.Min(_storedPower, amount);
            _storedPower -= removedAmount;
            return removedAmount;
        }

        protected virtual void HandleSyncEnabled(bool oldValue, bool newValue, bool asServer) { }
    }
}
