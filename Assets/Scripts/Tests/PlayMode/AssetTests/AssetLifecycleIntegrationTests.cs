using System.Collections;
using NUnit.Framework;
using SS3D.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests.PlayMode.AssetTests
{
    /// <summary>
    /// Full integration tests for the asset lifecycle chain:
    /// acquire → load → instantiate copy → destroy instance → auto-unload.
    /// Wires <see cref="AssetProvider"/>, <see cref="AssetLifecycleTracker"/>,
    /// and <see cref="InstanceLifetimeTracker"/> together as production code does.
    /// </summary>
    public sealed class AssetLifecycleIntegrationTests : Test
    {
        private FakeAssetBackend _backend;
        private AssetProvider _provider;
        private AssetLifecycleTracker _tracker;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _backend = new FakeAssetBackend();

            AssetLifecycleTracker tracker = null;
            _provider = new AssetProvider(key => tracker!.TrackRelease(key));
            tracker = new AssetLifecycleTracker(_provider);
            _tracker = tracker;
        }

        [TearDown]
        public override void TearDown()
        {
            _tracker.Shutdown();
            _provider.Dispose();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AcquireAndInstantiate_ThenDestroyInstance_UnloadsAsset()
        {
            CreateGameObject(out GameObject prefab);
            _backend.RegisterAsset("key1", prefab);

            _tracker.TrackAcquire("key1");
            AssetHandle<GameObject> handle = Acquire<GameObject>("key1");

            // Ref count = 2 (1 TrackAcquire + 1 instance from HandleAssetLoaded).
            Assert.That(_provider.IsLoaded("key1"), Is.True);

            // Release the handle ref (simulates AssetHandle.Dispose callback).
            handle.Dispose();

            // Ref count = 1 (instance ref remains).
            Assert.That(_provider.IsLoaded("key1"), Is.True, "Instance ref keeps asset alive");

            // Instantiate a copy — OnAfterDeserialize fires → ref count +1.
            GameObject copy = Object.Instantiate(prefab);
            instantiated.Add(copy);

            yield return null;

            // Ref count = 2 (1 original instance + 1 copy).
            Assert.That(_provider.IsLoaded("key1"), Is.True);

            // Simulate original prefab tracker release.
            _tracker.TrackRelease("key1");

            // Ref count = 1 (copy remains).
            Assert.That(_provider.IsLoaded("key1"), Is.True, "Copy keeps asset alive");

            // Destroy the copy — OnDestroy fires → OnReleased → ref count reaches 0.
            Object.DestroyImmediate(copy);

            Assert.That(_provider.IsLoaded("key1"), Is.False, "All refs released, asset unloaded");
            Assert.That(_backend.UnloadedKeys, Contains.Item("key1"));

            yield return null;
        }

        [UnityTest]
        public IEnumerator MultipleHandles_AllDisposed_InstanceKeepsAssetAlive()
        {
            CreateGameObject(out GameObject prefab);
            _backend.RegisterAsset("key1", prefab);

            _tracker.TrackAcquire("key1");
            _tracker.TrackAcquire("key1");
            AssetHandle<GameObject> handle1 = Acquire<GameObject>("key1");
            AssetHandle<GameObject> handle2 = Acquire<GameObject>("key1");

            // Ref count = 3 (2 TrackAcquire + 1 instance).
            handle1.Dispose();
            handle2.Dispose();

            // Ref count = 1 (instance ref remains).
            Assert.That(_provider.IsLoaded("key1"), Is.True);

            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyCopy_OriginalSurvives_AssetStaysLoaded()
        {
            CreateGameObject(out GameObject prefab);
            _backend.RegisterAsset("key1", prefab);

            _tracker.TrackAcquire("key1");
            Acquire<GameObject>("key1");

            GameObject copy = Object.Instantiate(prefab);
            instantiated.Add(copy);

            yield return null;

            // Destroy copy only.
            Object.DestroyImmediate(copy);

            // Original instance ref still alive.
            Assert.That(_provider.IsLoaded("key1"), Is.True);

            yield return null;
        }

        private AssetHandle<T> Acquire<T>(string key)
            where T : class
        {
            return _provider.AcquireAsync<T>(key, _backend).GetAwaiter().GetResult();
        }
    }
}
