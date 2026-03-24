using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Data;
using UnityEngine;

namespace SS3D.Tests.EditMode.AssetTests
{
    public sealed class AssetHandleTests
    {
        private readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                Object.DestroyImmediate(go);
            }

            _created.Clear();
        }

        [Test]
        public void IsValid_TrueWhenAssetPresent()
        {
            GameObject go = CreateGameObject();
            using AssetHandle<GameObject> handle = new("key", go, null);

            Assert.That(handle.IsValid, Is.True);
        }

        [Test]
        public void IsValid_FalseWhenAssetNull()
        {
            using AssetHandle<GameObject> handle = new("key", null, null);

            Assert.That(handle.IsValid, Is.False);
        }

        [Test]
        public void IsValid_FalseAfterDispose()
        {
            GameObject go = CreateGameObject();
            AssetHandle<GameObject> handle = new("key", go, null);
            handle.Dispose();

            Assert.That(handle.IsValid, Is.False);
        }

        [Test]
        public void Dispose_InvokesReleaseCallback()
        {
            string releasedKey = null;
            AssetHandle<GameObject> handle = new("my-key", CreateGameObject(), key => releasedKey = key);
            handle.Dispose();

            Assert.That(releasedKey, Is.EqualTo("my-key"));
        }

        [Test]
        public void Dispose_DoesNotInvokeCallbackTwice()
        {
            int callCount = 0;
            AssetHandle<GameObject> handle = new("key", CreateGameObject(), _ => callCount++);
            handle.Dispose();
            handle.Dispose();

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Key_ReturnsConstructorValue()
        {
            using AssetHandle<GameObject> handle = new("expected-key", null, null);

            Assert.That(handle.Key, Is.EqualTo("expected-key"));
        }

        private GameObject CreateGameObject()
        {
            GameObject go = new("TestAsset");
            _created.Add(go);
            return go;
        }
    }
}