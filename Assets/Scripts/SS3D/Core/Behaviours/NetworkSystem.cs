namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on networked objects that wont have two of the same time. Should not be instantiated at runtime.
    /// </summary>
    public class NetworkSystem : NetworkActor
    {
        /// <summary>
        /// Registers the system on awake.
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();
            SystemLocator.Register(this);
        }
    }
}