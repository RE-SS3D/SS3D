using System;

namespace SS3D.Systems.Health
{
    public class DamageTypeQuantity : ICloneable
    {
        private readonly DamageType _damageType;
        private readonly float _quantity;

        public DamageTypeQuantity(DamageType damageType, float quantity)
        {
            _damageType = damageType;
            _quantity = quantity;
        }

        public DamageType DamageType => _damageType;

        public float Quantity => _quantity;

        public object Clone()
        {
            return new DamageTypeQuantity(_damageType, _quantity);
        }
    }
}
