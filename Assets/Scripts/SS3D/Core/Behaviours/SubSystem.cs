namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Process-wide or world-scoped singleton service. One instance per type via <see cref="SubSystems"/>.
    /// Owned by <c>SystemsBootstrap</c> (DDOL) or scene/hub composition — not hand-placed ad hoc at runtime.
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