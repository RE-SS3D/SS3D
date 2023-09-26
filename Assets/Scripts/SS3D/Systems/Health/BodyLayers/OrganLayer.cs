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
		DamagesContainer damages)
		: base(bodyPart, damages)
		{

		}

		public override BodyLayerType LayerType { get => BodyLayerType.Organ; }

        public double GetOxygenNeeded()
        {
            return HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody * BodyPart.Volume;
        }

        protected override void SetDamagesContainer()
        {
            Damages.DamagesInfo.Add(DamageType.Crush, new BodyDamageInfo(DamageType.Crush, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Slash, new BodyDamageInfo(DamageType.Slash, 0f, 2f, 0f));
            Damages.DamagesInfo.Add(DamageType.Puncture, new BodyDamageInfo(DamageType.Puncture, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Pressure, new BodyDamageInfo(DamageType.Pressure, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Heat, new BodyDamageInfo(DamageType.Heat, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Cold, new BodyDamageInfo(DamageType.Cold, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Shock, new BodyDamageInfo(DamageType.Shock, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Rad, new BodyDamageInfo(DamageType.Rad, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Acid, new BodyDamageInfo(DamageType.Acid, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Toxic, new BodyDamageInfo(DamageType.Toxic, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Oxy, new BodyDamageInfo(DamageType.Oxy, 0f, 1f, 0f));
        }

        public override void Cleanlayer()
        {

        }
    }
}
