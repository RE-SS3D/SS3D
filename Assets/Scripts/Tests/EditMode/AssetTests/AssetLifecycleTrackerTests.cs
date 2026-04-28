using JetBrains.Annotations;
using NUnit.Framework;
using SS3D.Data;
using UnityEngine;

namespace SS3D.Tests.EditMode.AssetTests
{
    public sealed class AssetLifecycleTrackerTests : EditModeTest
    {
        private FakeAssetBackend _backend;
        private AssetProvider _provider;
        private AssetLifecycleTracker _tracker;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _backend = new();

            AssetLifecycleTracker tracker = null;
            _provider = new(key => tracker!.TrackRelease(key));
            tracker = new(_provider);

            // Production relays backend load events through AssetSubSystem; tests wire the fake backend directly.
            _backend.OnLoaded += tracker.TrackLoadedAsset;
            _tracker = tracker;
        }

        [TearDown]
        public override void TearDown()
        {
            _backend.OnLoaded -= _tracker.TrackLoadedAsset;
            _tracker.Shutdown();
            _provider.Dispose();
            base.TearDown();
        }

        [Test]
        public void TrackAcquire_ThenRelease_AssetSurvivesWhenInstanceExists()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            _tracker.TrackAcquire("key1");
            Acquire<GameObject>("key1");

            // HandleAssetLoaded adds InstanceLifetimeTracker → OnInstantiated fires → ref count +1.
            // So total ref count = 2 (1 TrackAcquire + 1 instance).
            // Release the acquire ref.
            _tracker.TrackRelease("key1");

            // Still alive because the instance ref remains.
            Assert.That(_provider.IsLoaded("key1"), Is.True);
        }

        [Test]
        public void AllRefsReleased_UnloadsAsset()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            _tracker.TrackAcquire("key1");
            Acquire<GameObject>("key1");

            // Ref count = 2 (1 TrackAcquire + 1 instance from HandleAssetLoaded).
            Assert.That(_provider.IsLoaded("key1"), Is.True);

            _tracker.TrackRelease("key1");
            Assert.That(_provider.IsLoaded("key1"), Is.True, "Instance ref still holds");

            // Simulate instance destruction — OnDestroy does not fire in EditMode
            // for MonoBehaviours without [ExecuteInEditMode], so we release directly.
            // The full DestroyImmediate → OnReleased → unload chain is tested in PlayMode.
            _tracker.TrackRelease("key1");

            Assert.That(_provider.IsLoaded("key1"), Is.False);
            Assert.That(_backend.UnloadedKeys, Contains.Item("key1"));
        }

        [Test]
        public void MultipleAcquires_RequireMatchingReleases()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            _tracker.TrackAcquire("key1");
            _tracker.TrackAcquire("key1");
            Acquire<GameObject>("key1");

            // Ref count = 3 (2 TrackAcquire + 1 instance).
            _tracker.TrackRelease("key1");
            Assert.That(_provider.IsLoaded("key1"), Is.True);

            _tracker.TrackRelease("key1");
            Assert.That(_provider.IsLoaded("key1"), Is.True, "Instance ref still holds");
        }

        [Test]
        public void TrackRelease_UnknownKey_NoOp()
        {
            Assert.DoesNotThrow(() => _tracker.TrackRelease("nonexistent"));
        }

        [Test]
        public void HandleAssetLoaded_InjectsTrackerOnGameObject()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            _tracker.TrackAcquire("key1");
            Acquire<GameObject>("key1");

            Assert.That(go.GetComponent<InstanceLifetimeTracker>(), Is.Not.Null);
        }

        [Test]
        public void HandleAssetLoaded_InitializesTrackerWithKey()
        {
            CreateGameObject(out GameObject go);
            _backend.RegisterAsset("key1", go);

            string instantiatedKey = null;

            InstanceLifetimeTracker.OnInstantiated += handler;

            try
            {
                _tracker.TrackAcquire("key1");
                Acquire<GameObject>("key1");
            }
            finally
            {
                InstanceLifetimeTracker.OnInstantiated -= handler;
            }

            Assert.That(instantiatedKey, Is.EqualTo("key1"));

            return;

            void handler(string key)
            {
                instantiatedKey = key;
            }
        }

        private AssetHandle<T> Acquire<T>([NotNull] string key)
            where T : class => _provider.AcquireAsync<T>(key, key, _backend).GetAwaiter().GetResult();
    }
}
