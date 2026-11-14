using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data.Persistence
{
    /// <summary>
    /// An object whose interface changes other objects' setup too: an air alarm's panel switches the vents and scrubbers
    /// in its room. The map editor's setup tracking snapshots these as well as the object itself, so undo reverts them.
    /// </summary>
    public interface ISetupScope
    {
        /// <summary>Server: adds the other objects this one's interface can change.</summary>
        void CollectSetupScope(List<GameObject> into);
    }
}
