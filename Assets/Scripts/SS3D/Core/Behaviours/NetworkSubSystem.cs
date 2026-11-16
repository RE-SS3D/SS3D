namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Networked singleton service. One instance per type via <see cref="SubSystems"/>.
    /// Live on the <c>NetworkSystemsHub</c> prefab (edit-time components); spawned with the hub Online —
    /// do not <c>AddComponent</c> networked subsystems at runtime.
    /// </summary>
    public class NetworkSubSystem : NetworkActor, ISubSystem
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