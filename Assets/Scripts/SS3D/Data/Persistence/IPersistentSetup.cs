namespace SS3D.Data.Persistence
{
    /// <summary>
    /// A component whose authored setup a station template keeps: a valve left open, a pump's target, a locker's lock,
    /// a container's contents (Documents/design/persistence-save.md §3).
    /// <para>
    /// <b>Setup only.</b> Capture what a builder chose through the object's own interface, never what the simulation
    /// accumulated: no stored resources (charge, fuel, tank gas), no damage, no live simulation state. Round snapshots
    /// get their own capture path; writing them through this interface would put charge and damage into templates
    /// (Documents/architecture/2026-11_object-setup-persistence.md Q22).
    /// </para>
    /// </summary>
    public interface IPersistentSetup
    {
        /// <summary>Unique among the setup components of one object; names the entry in the saved record.</summary>
        string SetupKey { get; }

        /// <summary>
        /// The current setup as JSON, or <c>null</c> when it matches the prefab default. Sparse on purpose: an object
        /// nobody configured follows the prefab default, including when that default later changes. Server only.
        /// </summary>
        string CaptureSetup();

        /// <summary>
        /// Applies setup written by <see cref="CaptureSetup"/>; <c>null</c> or empty returns the component to its prefab
        /// default. Runs on the server right after spawn, so it overrides what <c>OnStartServer</c> set, and again on
        /// undo and redo: calling it twice with the same JSON must leave the same state. Throws on JSON it can't read.
        /// </summary>
        void RestoreSetup(string json);
    }
}
