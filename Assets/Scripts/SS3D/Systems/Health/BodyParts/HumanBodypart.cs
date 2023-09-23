using SS3D.Systems.Health.BodyLayers;
using SS3D.Systems.Health.BodyParts;

namespace SS3D.Systems.Health
{
	/// <summary>
	/// Represent a generic body part for humans, without any particular mechanisms.
	/// </summary>
	public class HumanBodypart : BodyPart
	{
		protected override void AddInitialLayers()
		{
			TryAddBodyLayer(new MuscleLayer(this));
			TryAddBodyLayer(new BoneLayer(this));
            TryAddBodyLayer(new CirculatoryLayer(this,1f));
			TryAddBodyLayer(new NerveLayer(this));
			InvokeOnBodyPartLayerAdded();
		}

        protected override void AfterSpawningCopiedBodyPart()
        {
            return;
        }

        protected override void BeforeDestroyingBodyPart()
        {
            return;
        }
    }
}
