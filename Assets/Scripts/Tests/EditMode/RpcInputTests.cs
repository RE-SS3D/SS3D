using NUnit.Framework;
using SS3D.Core;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P1.5: the shared check every vector-taking ServerRpc runs on client input.</summary>
    public sealed class RpcInputTests
    {
        [Test]
        public void NonFinite_IsRejected()
        {
            Assert.IsFalse(RpcInput.IsFinite(float.NaN));
            Assert.IsFalse(RpcInput.IsFinite(float.PositiveInfinity));
            Assert.IsFalse(RpcInput.IsFinite(new Vector3(0f, float.NaN, 0f)));
            Assert.IsFalse(RpcInput.IsInStation(new Vector3(float.NegativeInfinity, 0f, 0f)));
            Assert.IsTrue(RpcInput.IsFinite(new Vector3(1f, 2f, 3f)));
        }

        [Test]
        public void StationVolume_HasSlackButAnEdge()
        {
            Assert.IsTrue(RpcInput.IsInStation(new Vector3(120f, 1f, -80f)));
            Assert.IsTrue(RpcInput.IsInStation(new Vector3(0f, StationBounds.FloorLimit - 1f, 0f)));
            Assert.IsFalse(RpcInput.IsInStation(new Vector3(0f, 100f, 0f)));
            Assert.IsFalse(RpcInput.IsInStation(new Vector3(RpcInput.MaxHorizontalExtent + 1f, 0f, 0f)));
        }

        [Test]
        public void Direction_MustBeNonZero()
        {
            Assert.IsTrue(RpcInput.IsDirection(Vector3.up));
            Assert.IsFalse(RpcInput.IsDirection(Vector3.zero));
            Assert.IsFalse(RpcInput.IsDirection(new Vector3(float.NaN, 1f, 0f)));
        }
    }
}
