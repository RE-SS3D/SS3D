using System.Collections.Generic;
using SS3D.Data;
using SS3D.Data.Generated;

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

        /// <summary>
        /// Initializes a new muscle layer and loads its base resistances and susceptibilities from the asset database.
        /// </summary>
        public MuscleLayer(BodyPart bodyPart) : base(bodyPart)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Muscle);
        }

        /// <summary>
        /// Initializes a new muscle layer with existing damages and modifiers, and loads its base data from the asset database.
        /// </summary>
        public MuscleLayer(BodyPart bodyPart, Dictionary<DamageType, float> damages,
            Dictionary<DamageType, float> susceptibilityModifiers, Dictionary<DamageType, float> resistanceModifiers)
            : base(bodyPart, damages, susceptibilityModifiers, resistanceModifiers)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Muscle);
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
