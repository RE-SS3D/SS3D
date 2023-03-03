namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on networked Actors that wont have two of the same type. Should not be instantiated at runtime.
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