using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// MonoBehaviour that auto-disposes tracked asset handles when its GameObject is destroyed.
    /// Attach via <see cref="AssetHandleExtensions.TieLifetimeTo"/>.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class HandleGuard : MonoBehaviour
    {
        private readonly List<IAssetHandle> _handles = new();

        internal void Track(IAssetHandle handle) => _handles.Add(handle);

        private void OnDestroy()
        {
            foreach (IAssetHandle handle in _handles)
            {
                handle.Dispose();
            }

            _handles.Clear();
        }
    }
}
