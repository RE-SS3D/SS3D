
using System.Collections.Generic;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Muscle layer mostly determines the ability to move and to hold things. 
    /// TODO : It should receive a top down signal from brain, and all muscle layer on the way influence 
    /// how much this one is able to function.
    /// </summary>
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
			DamagesContainer damages)
			: base(bodyPart, damages)
		{

		}

        protected override void SetDamagesContainer()
        {
            Damages.DamagesInfo.Add(DamageType.Crush, new BodyDamageInfo(DamageType.Crush, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Slash, new BodyDamageInfo(DamageType.Slash, 0f, 2f, 0f));
            Damages.DamagesInfo.Add(DamageType.Puncture, new BodyDamageInfo(DamageType.Puncture, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Pressure, new BodyDamageInfo(DamageType.Pressure, 0f, 0f, 0f));
            Damages.DamagesInfo.Add(DamageType.Heat, new BodyDamageInfo(DamageType.Heat, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Cold, new BodyDamageInfo(DamageType.Cold, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Shock, new BodyDamageInfo(DamageType.Shock, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Rad, new BodyDamageInfo(DamageType.Rad, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Acid, new BodyDamageInfo(DamageType.Acid, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Toxic, new BodyDamageInfo(DamageType.Toxic, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Oxy, new BodyDamageInfo(DamageType.Oxy, 0f, 1.5f, 0f));
        }

        public double GetOxygenNeeded()
        {
            return HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody * BodyPart.Volume;
        }

        public override void Cleanlayer()
        {

        }
    }
}
