using System;

namespace SS3D.Systems.Health
{
	public class DamageEventArgs : EventArgs
	{

		public DamageEventArgs(DamageTypeQuantity damageQuantity)
		{
			DamageTypeQuantity = damageQuantity;
		}
		public DamageTypeQuantity DamageTypeQuantity { get; set; }

	}
}
