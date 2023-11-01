using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// NerveLayer layer mostly determines the ability to feel things 
    /// TODO : It should send a down top signal to brain, representing how much pain it has.
    /// Too much pain should make the player drop things, or fall.
    /// </summary>
    public class NerveLayer : BodyLayer, IOxygenNeeder
	{

        public NetworkBehaviour GetNetworkBehaviour => BodyPart;


		public NetworkObject getNetworkedObject
		{
			get
			{
				return BodyPart.NetworkObject;
			}
			set
			{

			}
		}

		public GameObject getGameObject
		{
			get
			{
				return BodyPart.gameObject;
			}
			set
			{

			}
		}

		public override BodyLayerType LayerType
		{
			get { return BodyLayerType.Nerve; }
		}

		public NerveLayer(BodyPart bodyPart) : base(bodyPart)
		{

		}

		public NerveLayer(BodyPart bodyPart,
			DamagesContainer damages)
			: base(bodyPart, damages)
		{

		}

        protected override void SetDamagesContainer()
        {
            Damages.DamagesInfo.Add(DamageType.Crush, new BodyDamageInfo(DamageType.Crush, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Slash, new BodyDamageInfo(DamageType.Slash, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Puncture, new BodyDamageInfo(DamageType.Puncture, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Pressure, new BodyDamageInfo(DamageType.Pressure, 0f, 0.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Heat, new BodyDamageInfo(DamageType.Heat, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Cold, new BodyDamageInfo(DamageType.Cold, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Shock, new BodyDamageInfo(DamageType.Shock, 0f, 2f, 0f));
            Damages.DamagesInfo.Add(DamageType.Rad, new BodyDamageInfo(DamageType.Rad, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Acid, new BodyDamageInfo(DamageType.Acid, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Toxic, new BodyDamageInfo(DamageType.Toxic, 0f, 1.2f, 0f));
            Damages.DamagesInfo.Add(DamageType.Oxy, new BodyDamageInfo(DamageType.Oxy, 0f, 1f, 0f));
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
