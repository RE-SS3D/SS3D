using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
	public class BoneLayer : BodyLayer
	{
		public override BodyLayerType LayerType
		{
			get { return BodyLayerType.Bone; }
		}

		public BoneLayer(BodyPart bodyPart) : base(bodyPart)
		{

		}

		public BoneLayer(BodyPart bodyPart,
		List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
		: base(bodyPart, damages, susceptibilities, resistances)
		{

		}

		protected override void SetSuceptibilities()
		{
            DamageSuceptibilities.Add(new (DamageType.Crush, 2f));
            DamageSuceptibilities.Add(new (DamageType.Puncture, 1.5f));
            DamageSuceptibilities.Add(new (DamageType.Pressure, 0f));
            DamageSuceptibilities.Add(new (DamageType.Cold, 0.5f));
            DamageSuceptibilities.Add(new (DamageType.Shock, 0.5f));
            DamageSuceptibilities.Add(new (DamageType.Toxic, 0.8f));
            DamageSuceptibilities.Add(new (DamageType.Oxy, 0f));
		}

		/// <summary>
		/// Generate a certain amount of blood and put it in the circulatory system if there's one.
		/// </summary>
		public virtual void ProduceBlood()
		{
			throw new NotImplementedException();
		}

        public override void Cleanlayer()
        {
            
        }
    }
}
