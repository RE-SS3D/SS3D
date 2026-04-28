using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests.PlayMode.AssetTests
{
    /// <summary>
    /// PlayMode tests for <see cref="InstanceLifetimeTracker"/>.
    /// Covers paths that cannot be tested in EditMode:
    /// <list type="bullet">
    ///   <item><c>OnDestroy</c> firing <see cref="InstanceLifetimeTracker.OnReleased"/></item>
    ///   <item><see cref="ISerializationCallbackReceiver.OnAfterDeserialize"/> via
    ///         <c>Object.Instantiate</c> (requires <c>isPlaying == true</c>)</item>
    /// </list>
    /// </summary>
    public sealed class InstanceLifetimeTrackerPlayModeTests : Test
    {
        private readonly List<string> _instantiatedKeys = new();
        private readonly List<string> _releasedKeys = new();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            InstanceLifetimeTracker.OnInstantiated += HandleInstantiated;
            InstanceLifetimeTracker.OnReleased += HandleReleased;
        }

        [TearDown]
        public override void TearDown()
        {
            InstanceLifetimeTracker.OnInstantiated -= HandleInstantiated;
            InstanceLifetimeTracker.OnReleased -= HandleReleased;
            _instantiatedKeys.Clear();
            _releasedKeys.Clear();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Destroy_RaisesOnReleased()
        {
            CreateGameObject(out GameObject go, out InstanceLifetimeTracker tracker);
            tracker.Initialize("key1");
            Object.DestroyImmediate(go);

            yield return null;

            Assert.That(_releasedKeys, Contains.Item("key1"));
        }

        [UnityTest]
        public IEnumerator Destroy_DoesNotFireReleasedTwice()
        {
            CreateGameObject(out GameObject go, out InstanceLifetimeTracker tracker);
            tracker.Initialize("key1");
            Object.DestroyImmediate(go);

            yield return null;
            yield return null;

            Assert.That(_releasedKeys.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Destroy_UnannouncedTracker_DoesNotRelease()
        {
            CreateGameObject(out GameObject go, out InstanceLifetimeTracker _);
            Object.DestroyImmediate(go);

            yield return null;

            Assert.That(_releasedKeys, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Instantiate_RaisesOnInstantiated_ViaDeserialization()
        {
            CreateGameObject(out GameObject prefab, out InstanceLifetimeTracker tracker);
            tracker.Initialize("key1");
            _instantiatedKeys.Clear();

            GameObject copy = Object.Instantiate(prefab);
            instantiated.Add(copy);

            yield return null;

            Assert.That(_instantiatedKeys, Contains.Item("key1"));
        }

        [UnityTest]
        public IEnumerator Instantiate_ThenDestroyCopy_RaisesReleasedOnCopy()
        {
            CreateGameObject(out GameObject prefab, out InstanceLifetimeTracker tracker);
            tracker.Initialize("key1");
            _releasedKeys.Clear();

            GameObject copy = Object.Instantiate(prefab);
            instantiated.Add(copy);
            Object.DestroyImmediate(copy);

            yield return null;

            Assert.That(_releasedKeys, Contains.Item("key1"));
        }

        private void HandleInstantiated(string key) => _instantiatedKeys.Add(key);

        private void HandleReleased(string key) => _releasedKeys.Add(key);
    }
}