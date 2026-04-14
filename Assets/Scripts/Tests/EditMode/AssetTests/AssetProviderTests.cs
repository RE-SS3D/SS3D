using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Tests.EditMode.AssetTests
{
    public sealed class AssetProviderTests : EditModeTest
    {
        private FakeAssetBackend _backend;
        private AssetProvider _provider;
        private List<string> _releasedKeys;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _releasedKeys = new List<string>();
            _backend = new FakeAssetBackend();
            _provider = new AssetProvider(key => _releasedKeys.Add(key));
        }

        [TearDown]
        public override void TearDown()
        {
            _provider.Dispose();
            base.TearDown();
        }

        [Test]
        public void AcquireAsync_LoadsFromBackend()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            using AssetHandle<GameObject> handle = Acquire<GameObject>("key1");

            Assert.That(handle.Asset, Is.EqualTo(go));
            Assert.That(handle.IsValid, Is.True);
        }

        [Test]
        public void AcquireAsync_DeduplicatesConcurrentLoads()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            using AssetHandle<GameObject> handle1 = Acquire<GameObject>("key1");
            using AssetHandle<GameObject> handle2 = Acquire<GameObject>("key1");

            Assert.That(handle1.Asset, Is.EqualTo(handle2.Asset));
            Assert.That(_backend.LoadCallCount, Is.EqualTo(1));
        }

        [Test]
        public void AcquireAsync_RaisesOnLoaded()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            string loadedKey = null;
            Object loadedAsset = null;
            _backend.OnLoaded += (key, asset) =>
            {
                loadedKey = key;
                loadedAsset = asset;
            };

            using AssetHandle<GameObject> handle = Acquire<GameObject>("key1");

            Assert.That(loadedKey, Is.EqualTo("key1"));
            Assert.That(loadedAsset, Is.EqualTo(go));
        }

        [Test]
        public void AcquireAsync_OnLoaded_FiresOnlyOnce()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            int fireCount = 0;
            _backend.OnLoaded += (_, _) => fireCount++;

            using AssetHandle<GameObject> h1 = Acquire<GameObject>("key1");
            using AssetHandle<GameObject> h2 = Acquire<GameObject>("key1");

            Assert.That(fireCount, Is.EqualTo(1));
        }

        [Test]
        public void Unload_CallsBackendUnload()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);
            using AssetHandle<GameObject> handle = Acquire<GameObject>("key1");

            _provider.Unload("key1");

            Assert.That(_backend.UnloadedKeys, Contains.Item("key1"));
        }

        [Test]
        public void Unload_RaisesOnUnloaded()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);
            using AssetHandle<GameObject> handle = Acquire<GameObject>("key1");

            string unloadedKey = null;
            _backend.OnUnloaded += key => unloadedKey = key;
            _provider.Unload("key1");

            Assert.That(unloadedKey, Is.EqualTo("key1"));
        }

        [Test]
        public void Unload_UnknownKey_NoOp()
        {
            Assert.DoesNotThrow(() => _provider.Unload("nonexistent"));
            Assert.That(_backend.UnloadedKeys, Is.Empty);
        }

        [Test]
        public void IsLoaded_TrueAfterAcquire()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);
            using AssetHandle<GameObject> handle = Acquire<GameObject>("key1");

            Assert.That(_provider.IsLoaded("key1"), Is.True);
        }

        [Test]
        public void IsLoaded_FalseAfterUnload()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);
            using AssetHandle<GameObject> handle = Acquire<GameObject>("key1");
            _provider.Unload("key1");

            Assert.That(_provider.IsLoaded("key1"), Is.False);
        }

        [Test]
        public void IsLoaded_FalseForUnknownKey()
        {
            Assert.That(_provider.IsLoaded("nonexistent"), Is.False);
        }

        [Test]
        public void Dispose_UnloadsAllLoadedAssets()
        {
            CreateGameObject(out GameObject go1);
            CreateGameObject(out GameObject go2);
            _backend.RegisterAsset("key1", go1);
            _backend.RegisterAsset("key2", go2);

            using AssetHandle<GameObject> h1 = Acquire<GameObject>("key1");
            using AssetHandle<GameObject> h2 = Acquire<GameObject>("key2");

            _provider.Dispose();

            Assert.That(_backend.UnloadedKeys, Contains.Item("key1"));
            Assert.That(_backend.UnloadedKeys, Contains.Item("key2"));
        }

        [Test]
        public void Handle_Dispose_InvokesReleaseCallback()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);
            AssetHandle<GameObject> handle = Acquire<GameObject>("key1");
            handle.Dispose();

            Assert.That(_releasedKeys, Contains.Item("key1"));
        }

        private AssetHandle<T> Acquire<T>(string key)
            where T : class
        {
            return _provider.AcquireAsync<T>(key, key, _backend).GetAwaiter().GetResult();
        }
    }
}