
using System.Collections.Generic;

namespace SS3D.Systems.Health
{
	public class MuscleLayer : BodyLayer, IOxygenNeeder
	{
		public override BodyLayerType LayerType
		{
			get { return BodyLayerType.Muscle; }
		}

		public MuscleLayer(BodyPart bodyPart) : base(bodyPart)
		{

		}

		public MuscleLayer(BodyPart bodyPart,
			List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
			: base(bodyPart, damages, susceptibilities, resistances)
		{

		}

		protected override void SetSuceptibilities()
		{
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Pressure, 0f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Heat, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Cold, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Shock, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Acid, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Oxy, 1.5f));
		}

        public double GetOxygenNeeded()
        {
            return HealthConstants.MilliMolesPerCentilitersOfOxygen * BodyPart.Volume * 1000;
        }

        public override void Cleanlayer()
        {

        }
    }
}
