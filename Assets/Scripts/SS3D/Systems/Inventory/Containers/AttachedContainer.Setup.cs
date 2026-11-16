using FishNet.Object;
using SS3D.Data.Persistence;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>A container's contents as authored setup; the work is in <see cref="ContainerContentsSetup"/>.</summary>
    public partial class AttachedContainer : IPersistentSetup
    {
        public string SetupKey => ContainerContentsSetup.KeyFor(this);

        public string CaptureSetup() => ContainerContentsSetup.Capture(this);

        [Server]
        public void RestoreSetup(string json) => ContainerContentsSetup.Restore(this, json);

        /// <summary>Server: takes every stored item out, past any removal lock, for a setup restore to replace them.</summary>
        [Server]
        internal List<Item> ServerTakeAllForSetup()
        {
            var taken = new List<Item>(_storedItems.Count);
            for (int i = _storedItems.Count - 1; i >= 0; i--)
            {
                Item item = _storedItems[i].Item;
                RemoveStoredItem(i, ignoreConditions: true);
                if (item != null)
                    taken.Add(item);
            }

            return taken;
        }
    }
}
