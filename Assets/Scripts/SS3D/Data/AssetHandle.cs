using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Non-generic companion providing convenience helpers for <see cref="AssetHandle{T}"/>.
    /// </summary>
    public static class AssetHandle
    {
        /// <summary>
        /// Disposes the given handle and sets the reference to <see langword="null"/>,
        /// ensuring the caller cannot accidentally reuse a released handle.
        /// Safe to call even when <paramref name="handle"/> is already <see langword="null"/>.
        /// </summary>
        /// <param name="handle">The handle to release. Set to <see langword="null"/> after disposal.</param>
        /// <typeparam name="T">The asset type held by the handle.</typeparam>
        public static void Release<T>([CanBeNull] ref AssetHandle<T> handle)
            where T : class
        {
            handle?.Dispose();
            handle = null;
        }
    }

    /// <summary>
    /// RAII ownership handle for a loaded asset. One handle equals one ref-count contribution.
    /// Disposing the handle releases the ref count. If the count drops to zero the asset is unloaded.
    /// <para>
    /// Handles are decoupled from the provider via a release delegate injected at creation time.
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

        public static implicit operator bool(AssetHandle<T> handle) => handle is { IsValid: true };

        /// <summary>
        /// Ties this handle's lifetime to a <see cref="Component"/>.
        /// The handle is automatically disposed when the component's GameObject is destroyed.
        /// </summary>
        public void TieLifetimeTo([NotNull] Component owner)
        {
            if (!owner.TryGetComponent(out HandleGuard guard))
            {
                guard = owner.gameObject.AddComponent<HandleGuard>();
            }

            guard.Track(this);
        }

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
