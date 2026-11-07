using System.Collections.Generic;
using SS3D.Data;
using SS3D.Data.Generated;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Body layer present on internal organs (such as brain, heart, and lungs).
    /// </summary>
    public class OrganLayer : BodyLayer, IOxygenNeeder
    {
        /// <summary>
        /// Initializes a new organ layer and loads its base resistances and susceptibilities from the asset database.
        /// </summary>
        public OrganLayer(BodyPart bodyPart) : base(bodyPart)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Organ);
        }

        /// <summary>
        /// Initializes a new organ layer with existing damages and modifiers, and loads its base data from the asset database.
        /// </summary>
        public OrganLayer(BodyPart bodyPart, Dictionary<DamageType, float> damages,
            Dictionary<DamageType, float> susceptibilityModifiers, Dictionary<DamageType, float> resistanceModifiers)
            : base(bodyPart, damages, susceptibilityModifiers, resistanceModifiers)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Organ);
        }

        public override BodyLayerType LayerType => BodyLayerType.Organ;

        public double GetOxygenNeeded()
        {
            return HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody * BodyPart.Volume;
        }

        public override void Cleanlayer()
        {
        }
    }
}
