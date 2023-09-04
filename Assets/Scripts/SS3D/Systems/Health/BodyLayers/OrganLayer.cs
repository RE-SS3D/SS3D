using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
	public class OrganLayer : BodyLayer, IOxygenNeeder
    {
        public OrganLayer(BodyPart bodyPart) : base(bodyPart)
		{
		}

		public OrganLayer(BodyPart bodyPart,
		List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
		: base(bodyPart, damages, susceptibilities, resistances)
		{

		}

		public override BodyLayerType LayerType { get => BodyLayerType.Organ; }

        public double GetOxygenNeeded()
        {
            return HealthConstants.MilliMolesPerCentilitersOfOxygen * BodyPart.Volume * 1000;
        }

        protected override void SetSuceptibilities()
		{
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Heat, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Cold, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Shock, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Rad, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Acid, 1.5f));
		}

        public override void Cleanlayer()
        {

        }
    }
}
