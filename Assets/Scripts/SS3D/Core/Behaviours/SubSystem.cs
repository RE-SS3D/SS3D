namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on objects that won't have two of them at the same time.
    /// Should not be instantiated at runtime.
    /// </summary>
    public class SubSystem : Actor, ISubSystem
    {
        /// <summary>
        /// Registers the system on awake.
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();

            SubSystems.Register(this);
        }

        /// <summary>
        /// Unregisters the system on destroyed.
        /// </summary>
        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            SubSystems.Unregister(this);
        }
    }
}