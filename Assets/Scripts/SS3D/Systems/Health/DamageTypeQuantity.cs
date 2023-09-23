using System;

namespace SS3D.Systems.Health
{
	public class DamageTypeQuantity : ICloneable
	{
		public DamageType DamageType;
		public float Quantity;

		public DamageTypeQuantity(DamageType damageType, float quantity)
		{
			DamageType = damageType;
			Quantity = quantity;
		}

		public object Clone()
		{
			return new DamageTypeQuantity(DamageType, Quantity);
		}
	}
}
