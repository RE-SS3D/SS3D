using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using Object = UnityEngine.Object;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Pure-logic tests for <see cref="AssetProvider"/> ref-counting with a fake Addressables backend.
    /// </summary>
    public class AssetProviderTests
    {
        private FakeAssetLoadBackend _backend;
        private Texture2D _texA;
        private Texture2D _texB;

        [SetUp]
        public void SetUp()
        {
            _texA = new Texture2D(1, 1);
            _texB = new Texture2D(1, 1);
            _backend = new FakeAssetLoadBackend
            {
                Assets =
                {
                    ["key-a"] = _texA,
                    ["key-b"] = _texB,
                },
            };
            AssetProvider.SetBackend(_backend);
        }

        [TearDown]
        public void TearDown()
        {
            AssetProvider.ResetToAddressablesBackend();
            Object.DestroyImmediate(_texA);
            Object.DestroyImmediate(_texB);
        }

        [Test]
        public async Task TwoAcquiresShareOneLoad()
        {
            AssetHandle<Texture2D> first = await AssetProvider.AcquireAsync<Texture2D>("key-a");
            AssetHandle<Texture2D> second = await AssetProvider.AcquireAsync<Texture2D>("key-a");

            Assert.AreSame(_texA, first.Asset);
            Assert.AreSame(_texA, second.Asset);
            Assert.AreEqual(1, _backend.LoadCount);

            first.Release();
            Assert.AreEqual(0, _backend.ReleaseCount);

            second.Release();
            Assert.AreEqual(1, _backend.ReleaseCount);
        }

        [Test]
        public async Task DoubleReleaseIsSafe()
        {
            AssetHandle<Texture2D> handle = await AssetProvider.AcquireAsync<Texture2D>("key-a");
            handle.Release();
            handle.Release();

            Assert.AreEqual(1, _backend.ReleaseCount);
        }

        [Test]
        public async Task MissingKeyFailsLoudly()
        {
            // Await UniTask directly. Assert.ThrowsAsync + UniTask can leave ExceptionHolder
            // unobserved; AssetProvider must also not TrySetException without waiters (see AcquireAsync).
            try
            {
                await AssetProvider.AcquireAsync<Texture2D>("missing");
                Assert.Fail("Expected InvalidOperationException for a missing Addressables key.");
            }
            catch (InvalidOperationException ex)
            {
                StringAssert.Contains("missing", ex.Message);
            }
        }

        [Test]
        public async Task ConcurrentMissingKeyDoesNotLeakUnobservedException()
        {
            _backend.LoadDelayMs = 30;

            UniTask<AssetHandle<Texture2D>> t1 = AssetProvider.AcquireAsync<Texture2D>("missing");
            UniTask<AssetHandle<Texture2D>> t2 = AssetProvider.AcquireAsync<Texture2D>("missing");

            int failures = 0;
            try
            {
                await t1;
            }
            catch (InvalidOperationException)
            {
                failures++;
            }

            try
            {
                await t2;
            }
            catch (InvalidOperationException)
            {
                failures++;
            }

            Assert.AreEqual(2, failures);
            Assert.AreEqual(1, _backend.LoadCount);
        }

        [Test]
        public void TryGetCachedAfterPreload()
        {
            AssetProvider.PreloadAndHoldSync(new[] { "key-a", "key-b" });

            Assert.IsTrue(AssetProvider.TryGetCached("key-a", out Texture2D a));
            Assert.AreSame(_texA, a);
            Assert.IsTrue(AssetProvider.TryGetCached("key-b", out Texture2D b));
            Assert.AreSame(_texB, b);
            Assert.AreEqual(2, _backend.LoadCount);
        }

        [Test]
        public async Task ConcurrentAcquiresDedupeLoad()
        {
            _backend.LoadDelayMs = 50;

            UniTask<AssetHandle<Texture2D>> t1 = AssetProvider.AcquireAsync<Texture2D>("key-a");
            UniTask<AssetHandle<Texture2D>> t2 = AssetProvider.AcquireAsync<Texture2D>("key-a");

            (AssetHandle<Texture2D> first, AssetHandle<Texture2D> second) = await UniTask.WhenAll(t1, t2);

            Assert.AreSame(_texA, first.Asset);
            Assert.AreSame(_texA, second.Asset);
            Assert.AreEqual(1, _backend.LoadCount);

            first.Release();
            second.Release();
            Assert.AreEqual(1, _backend.ReleaseCount);
        }

        private sealed class FakeAssetLoadBackend : IAssetLoadBackend
        {
            public readonly Dictionary<string, Object> Assets = new();
            public int LoadCount;
            public int ReleaseCount;
            public int LoadDelayMs;

            public async UniTask<(TAsset Asset, object ReleaseToken)> LoadAsync<TAsset>(string key)
                where TAsset : Object
            {
                if (LoadDelayMs > 0)
                {
                    // EditMode has no PlayerLoop — UniTask.Delay (even Realtime) can stall or take
                    // minutes under -batchmode; System.Threading.Tasks timers keep working.
                    await Task.Delay(LoadDelayMs);
                }

                return LoadSync<TAsset>(key);
            }

            public (TAsset Asset, object ReleaseToken) LoadSync<TAsset>(string key)
                where TAsset : Object
            {
                LoadCount++;
                if (!Assets.TryGetValue(key, out Object obj) || obj is not TAsset typed)
                {
                    throw new InvalidOperationException($"Addressables failed to load key '{key}' as {typeof(TAsset).Name}.");
                }

                return (typed, key);
            }

            public void Release(object releaseToken)
            {
                ReleaseCount++;
            }
        }
    }
}
