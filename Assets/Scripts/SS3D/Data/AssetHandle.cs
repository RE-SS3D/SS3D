using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// RAII ownership handle for a loaded asset. One handle equals one ref-count contribution.
    /// Disposing the handle releases the ref count. If the count drops to zero the asset is unloaded.
    /// <para>
    /// Handles are decoupled from the store via a release delegate injected at creation time.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the loaded asset (e.g. <see cref="GameObject"/>).</typeparam>
    public sealed class AssetHandle<T> : IAssetHandle
        where T : class
    {
        [CanBeNull] private readonly Action<string> _onRelease;
        private bool _disposed;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private readonly string _creationTrace;
#endif

        internal AssetHandle([NotNull] string key, [CanBeNull] T asset, [CanBeNull] Action<string> onRelease)
        {
            Key = key;
            Asset = asset;
            _onRelease = onRelease;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _creationTrace = Environment.StackTrace;
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        ~AssetHandle()
        {
            if (!_disposed)
            {
                Debug.LogWarning($"AssetHandle for '{Key}' was never disposed. Creation trace:\n{_creationTrace}");
            }
        }
#endif

        /// <summary>
        /// The loaded asset. May be <see langword="null"/> if the load failed.
        /// </summary>
        [CanBeNull]
        public T Asset { get; }

        /// <inheritdoc/>
        public string Key { get; }

        /// <inheritdoc/>
        public bool IsValid => !_disposed && Asset != null;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _onRelease?.Invoke(Key);
            GC.SuppressFinalize(this);
        }
    }
}
