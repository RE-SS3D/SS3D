using DG.Tweening.Core.Easing;
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

		public BoneLayer(BodyPart bodyPart, DamagesContainer damages)
		: base(bodyPart, damages)
		{

		}

        protected override void SetDamagesContainer()
        {
            Damages.DamagesInfo.Add(DamageType.Crush, new BodyDamageInfo(DamageType.Crush, 0f, 2f, 0f));
            Damages.DamagesInfo.Add(DamageType.Slash, new BodyDamageInfo(DamageType.Slash, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Puncture, new BodyDamageInfo(DamageType.Puncture, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Pressure, new BodyDamageInfo(DamageType.Pressure, 0f, 0.2f, 0f));
            Damages.DamagesInfo.Add(DamageType.Heat, new BodyDamageInfo(DamageType.Heat, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Cold, new BodyDamageInfo(DamageType.Cold, 0f, 0.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Shock, new BodyDamageInfo(DamageType.Shock, 0f, 0.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Rad, new BodyDamageInfo(DamageType.Rad, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Acid, new BodyDamageInfo(DamageType.Acid, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Toxic, new BodyDamageInfo(DamageType.Toxic, 0f, 0.8f, 0f));
            Damages.DamagesInfo.Add(DamageType.Oxy, new BodyDamageInfo(DamageType.Oxy, 0f, 0f, 0f));
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
