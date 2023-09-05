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
            return HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody * BodyPart.Volume;
        }

        public override void Cleanlayer()
        {

        }
    }
}
