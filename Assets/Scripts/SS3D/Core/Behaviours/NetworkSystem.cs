namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on networked objects that wont have two of the same time. Should not be instantiated at runtime.
    /// </summary>
    public class NetworkSystem : NetworkActor
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