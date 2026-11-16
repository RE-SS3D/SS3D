using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Extension seam for the main disposal outlet's Cargo hook (design doc §6). Cargo doesn't exist
    /// in code yet, so this ships with no real subscriber — a future Cargo export-pad system can
    /// implement/subscribe to redirect an unclaimed item to the export pad instead of space ejection.
    /// </summary>
    public interface IDisposalSweepable
    {
        /// <summary>
        /// Called when an unclaimed item's grace window is about to expire. Returning true claims the
        /// item (e.g. onto the export pad) and cancels space ejection.
        /// </summary>
        bool TrySweep(DisposalOutlet outlet, Item item);
    }
}
