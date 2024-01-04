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
        private float _maxPowerRate = 15f;

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
        public float MaxRemovablePower { get => Math.Min(_storedPower, _maxPowerRate); }

        /// <inheritdoc> </inheritdoc>
        public bool IsOn { get => _isOn; set => _isOn = value; }

        public void Init(float maxPowerRate, float maxCapacity, float storedPower)
        {
            _maxPowerRate = Mathf.Max(0, maxPowerRate);
            _maxCapacity = Mathf.Max(0, maxCapacity);
            StoredPower = storedPower;
        }


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
