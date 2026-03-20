using System;
using JetBrains.Annotations;

namespace SS3D.Data
{
    /// <summary>
    /// Non-generic handle interface for lifetime management (e.g. <see cref="HandleGuard"/>).
    /// </summary>
    public interface IAssetHandle : IDisposable
    {
        /// <summary>
        /// The key that identifies the loaded asset in the store.
        /// </summary>
        [NotNull]
        string Key { get; }

        /// <summary>
        /// <see langword="true"/> while the handle has not been disposed and the asset is still alive.
        /// </summary>
        bool IsValid { get; }
    }
}
