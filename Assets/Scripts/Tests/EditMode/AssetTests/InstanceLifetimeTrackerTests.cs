using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Data;
using UnityEngine;

namespace SS3D.Tests.EditMode.AssetTests
{
    /// <summary>
    /// EditMode tests for <see cref="InstanceLifetimeTracker"/>.
    /// Only the <see cref="InstanceLifetimeTracker.Initialize"/> path is testable here.
    /// <see cref="ISerializationCallbackReceiver.OnAfterDeserialize"/> returns early in EditMode
    /// because <c>EditorApplication.isPlaying</c> is false, and <c>OnDestroy</c> is not called
    /// for MonoBehaviours without <c>[ExecuteInEditMode]</c>.
    /// PlayMode tests cover both paths.
    /// </summary>
    public sealed class InstanceLifetimeTrackerTests
    {
        private readonly List<GameObject> _created = new();
        private readonly List<string> _instantiatedKeys = new();

        [SetUp]
        public void SetUp()
        {
            InstanceLifetimeTracker.OnInstantiated += HandleInstantiated;
        }

        [TearDown]
        public void TearDown()
        {
            InstanceLifetimeTracker.OnInstantiated -= HandleInstantiated;

            foreach (GameObject go in _created)
            {
                if (go)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _created.Clear();
            _instantiatedKeys.Clear();
        }

        [Test]
        public void Initialize_RaisesOnInstantiated()
        {
            InstanceLifetimeTracker tracker = CreateTracker();
            tracker.Initialize("key1");

            Assert.That(_instantiatedKeys, Contains.Item("key1"));
        }

        [Test]
        public void Initialize_DoesNotFireTwice()
        {
            InstanceLifetimeTracker tracker = CreateTracker();
            tracker.Initialize("key1");
            tracker.Initialize("key1");

            Assert.That(_instantiatedKeys.Count, Is.EqualTo(1));
        }

        private InstanceLifetimeTracker CreateTracker()
        {
            GameObject go = new("TrackerTest");
            _created.Add(go);
            return go.AddComponent<InstanceLifetimeTracker>();
        }

        private void HandleInstantiated(string key) => _instantiatedKeys.Add(key);
    }
}