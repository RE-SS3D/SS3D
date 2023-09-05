namespace SS3D.Systems.Health
{
	/// <summary>
	/// Feet are necessary to walk, depending on how damaged they are, the player will slow down.
	/// Feet need a humanoid controller as a parent, as they will send info about their state to it, to determine how fast
	/// a player can walk, as well as determining limping animation and stuff.
	/// </summary>
	public class FootBodyPart : BodyPart, IWalkEnabler
	{
		public float GetSpeedContribution()
		{
			return 1-RelativeDamage;
		}

		protected override void AddInitialLayers()
		{
			TryAddBodyLayer(new MuscleLayer(this));
			TryAddBodyLayer(new BoneLayer(this));
			TryAddBodyLayer(new CirculatoryLayer(this,5f));
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
