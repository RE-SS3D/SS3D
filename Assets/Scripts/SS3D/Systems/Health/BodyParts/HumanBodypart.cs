namespace SS3D.Systems.Health
{
	/// <summary>
	/// Represent a generic body part for humans, without any particular mechanisms.
	/// </summary>
	public class HumanBodypart : BodyPart
	{
		public override void Init(BodyPart parent)
		{
			base.Init(parent);
		}

		protected override void AddInitialLayers()
		{
			TryAddBodyLayer(new MuscleLayer(this));
			TryAddBodyLayer(new BoneLayer(this));
			TryAddBodyLayer(new CirculatoryLayer(this));
			TryAddBodyLayer(new NerveLayer(this));
			InvokeOnBodyPartLayerAdded();
		}
	}
}
