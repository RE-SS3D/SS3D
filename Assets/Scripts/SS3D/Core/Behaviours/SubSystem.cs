namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on objects that wont have two of the same time. Should not be instantiated at runtime.
    /// </summary>
    public class SubSystem : Actor
    {
        protected override void OnAwake()
        {
            base.OnAwake();
            Subsystems.Register(this);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            Subsystems.Unregister(this);
        }
    }
}