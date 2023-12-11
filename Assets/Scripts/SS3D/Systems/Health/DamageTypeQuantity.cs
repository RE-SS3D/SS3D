using System;

namespace SS3D.Systems.Health
{
	public class DamageTypeQuantity : ICloneable
	{
		public DamageType damageType;
		public float quantity;

		public DamageTypeQuantity(DamageType damageType, float quantity)
		{
			this.damageType = damageType;
			this.quantity = quantity;
		}

		public object Clone()
		{
			return new DamageTypeQuantity(damageType, quantity);
		}
	}
}
