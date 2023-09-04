using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

namespace SS3D.Systems.Health
{
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
			List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
			: base(bodyPart, damages, susceptibilities, resistances)
		{

		}

		protected override void SetSuceptibilities()
		{
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Pressure, 0.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Shock, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Rad, 1.5f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 1.2f));
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
