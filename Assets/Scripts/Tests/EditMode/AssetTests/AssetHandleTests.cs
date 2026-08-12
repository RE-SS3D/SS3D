using NUnit.Framework;
using SS3D.Data;
using UnityEngine;

namespace SS3D.Tests.EditMode.AssetTests
{
    public sealed class AssetHandleTests : EditModeTest
    {
        [Test]
        public void IsValid_TrueWhenAssetPresent()
        {
            CreateGameObject(out GameObject go);
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
            CreateGameObject(out GameObject go);
            AssetHandle<GameObject> handle = new("key", go, null);
            handle.Dispose();

            Assert.That(handle.IsValid, Is.False);
        }

        [Test]
        public void Dispose_InvokesReleaseCallback()
        {
            CreateGameObject(out GameObject go);
            string releasedKey = null;
            AssetHandle<GameObject> handle = new("my-key", go, key => releasedKey = key);
            handle.Dispose();

            Assert.That(releasedKey, Is.EqualTo("my-key"));
        }

        [Test]
        public void Dispose_DoesNotInvokeCallbackTwice()
        {
            CreateGameObject(out GameObject go);
            int callCount = 0;
            AssetHandle<GameObject> handle = new("key", go, _ => callCount++);
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
    }
}