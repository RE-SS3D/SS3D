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
    public sealed class InstanceLifetimeTrackerTests : EditModeTest
    {
        private readonly List<string> _instantiatedKeys = new();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            InstanceLifetimeTracker.OnInstantiated += HandleInstantiated;
        }

        [TearDown]
        public override void TearDown()
        {
            InstanceLifetimeTracker.OnInstantiated -= HandleInstantiated;
            _instantiatedKeys.Clear();
            base.TearDown();
        }

        [Test]
        public void Initialize_RaisesOnInstantiated()
        {
            CreateGameObject<InstanceLifetimeTracker>(out _, out InstanceLifetimeTracker tracker);
            tracker.Initialize("key1");

            Assert.That(_instantiatedKeys, Contains.Item("key1"));
        }

        [Test]
        public void Initialize_DoesNotFireTwice()
        {
            CreateGameObject<InstanceLifetimeTracker>(out _, out InstanceLifetimeTracker tracker);
            tracker.Initialize("key1");
            tracker.Initialize("key1");

            Assert.That(_instantiatedKeys.Count, Is.EqualTo(1));
        }

        private void HandleInstantiated(string key) => _instantiatedKeys.Add(key);
    }
}