namespace SS3D.Systems.Health
{
    public class HandBodyPart : BodyPart
    {
        protected override void AddInitialLayers()
        {
            TryAddBodyLayer(new MuscleLayer(this));
            TryAddBodyLayer(new BoneLayer(this));
            TryAddBodyLayer(new CirculatoryLayer(this, 5f));
            TryAddBodyLayer(new NerveLayer(this));
            InvokeOnBodyPartLayerAdded();
        }

        protected override void AfterSpawningCopiedBodyPart() { }

        protected override void BeforeDestroyingBodyPart() { }
    }
}
