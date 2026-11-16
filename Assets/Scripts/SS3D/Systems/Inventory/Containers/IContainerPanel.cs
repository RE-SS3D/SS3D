namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Handle to whatever UI surface currently displays a container's contents, so
    /// ContainerInteractive can ask it to close without depending on a concrete UI type
    /// (replaces the old condemned ContainerUi reference — see StoragePanelView).
    /// </summary>
    public interface IContainerPanel
    {
        /// <summary>
        /// Local visual teardown only — called when the container's own open state changes
        /// server-side. Must not re-request a server close (the server already knows).
        /// </summary>
        void Close();
    }
}
