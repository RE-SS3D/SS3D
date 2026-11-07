using System.Collections.Generic;
using FishNet.Object;
using SS3D.Data;
using SS3D.Data.Generated;
using UnityEngine;

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

        /// <summary>
        /// Initializes a new nerve layer and loads its base resistances and susceptibilities from the asset database.
        /// </summary>
        public NerveLayer(BodyPart bodyPart) : base(bodyPart)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Nerve);
        }

        /// <summary>
        /// Initializes a new nerve layer with existing damages and modifiers, and loads its base data from the asset database.
        /// </summary>
        public NerveLayer(BodyPart bodyPart, Dictionary<DamageType, float> damages,
            Dictionary<DamageType, float> susceptibilityModifiers, Dictionary<DamageType, float> resistanceModifiers)
            : base(bodyPart, damages, susceptibilityModifiers, resistanceModifiers)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Nerve);
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
