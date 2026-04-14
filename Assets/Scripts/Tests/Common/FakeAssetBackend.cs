using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SS3D.Data;
using Object = UnityEngine.Object;

namespace SS3D.Tests
{
    public sealed class FakeAssetBackend : IAssetBackend
    {
        public event Action<string, Object> OnLoaded;
        public event Action<string> OnUnloaded;

        private readonly Dictionary<string, Object> _assets = new();

        public List<string> UnloadedKeys { get; } = new();
        public int LoadCallCount { get; private set; }

        public void RegisterAsset(string key, Object asset) => _assets[key] = asset;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task<Object> LoadAsync(string key)
        {
            LoadCallCount++;
            _assets.TryGetValue(key, out Object asset);

            if (asset)
            {
                OnLoaded?.Invoke(key, asset);
            }

            return Task.FromResult(asset);
        }

        public void Unload(string key)
        {
            UnloadedKeys.Add(key);
            OnUnloaded?.Invoke(key);
        }

        public void Dispose() { }
    }
}