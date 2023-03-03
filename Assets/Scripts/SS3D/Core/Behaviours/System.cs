namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on objects that wont have two of the same time. Should not be instantiated at runtime.
    /// </summary>
    public class System : Actor
    {
        internal override void OnEnable()
        {
            base.OnEnable();

            SystemLocator.Register(this);
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            SystemLocator.Unregister(this);
        }
    }
}