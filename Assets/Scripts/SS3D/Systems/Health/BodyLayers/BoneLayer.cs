using System;
using System.Collections.Generic;
using SS3D.Data;
using SS3D.Data.Generated;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Structural body layer representing bones. When destroyed, detachable body parts are severed.
    /// </summary>
    public class BoneLayer : BodyLayer
    {
        public override BodyLayerType LayerType
        {
            get { return BodyLayerType.Bone; }
        }

        /// <summary>
        /// Initializes a new bone layer and loads its base resistances and susceptibilities from the asset database.
        /// </summary>
        public BoneLayer(BodyPart bodyPart) : base(bodyPart)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Bone);
        }

        /// <summary>
        /// Initializes a new bone layer with existing damages and modifiers, and loads its base data from the asset database.
        /// </summary>
        public BoneLayer(BodyPart bodyPart, Dictionary<DamageType, float> damages,
            Dictionary<DamageType, float> susceptibilityModifiers, Dictionary<DamageType, float> resistanceModifiers)
            : base(bodyPart, damages, susceptibilityModifiers, resistanceModifiers)
        {
            _bodyLayerData = Assets.Get<BodyLayerData>(AssetDatabases.BodyLayers, BodyLayers.Bone);
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
