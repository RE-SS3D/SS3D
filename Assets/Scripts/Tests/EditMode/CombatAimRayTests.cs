using NUnit.Framework;
using SS3D.Systems.Combat;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P1.1: shots and swings start at the server's eye; the client only picks the aim point.</summary>
    public sealed class CombatAimRayTests
    {
        [Test]
        public void Ray_StartsAtTheServerEye_AndPointsAtTheAim()
        {
            Vector3 eye = new(0f, 1.6f, 0f);
            Vector3 aim = new(0f, 1.6f, 10f);
            Assert.IsTrue(CombatAimRay.TryBuild(eye, aim, out Ray ray));
            Assert.AreEqual(eye, ray.origin);
            Assert.AreEqual(1f, Vector3.Dot(ray.direction, Vector3.forward), 1e-5f);
        }

        [Test]
        public void AimPastRange_KeepsTheDirection()
        {
            Assert.IsTrue(CombatAimRay.TryBuild(Vector3.zero, new Vector3(500f, 0f, 0f), out Ray ray));
            Assert.AreEqual(1f, Vector3.Dot(ray.direction, Vector3.right), 1e-5f);
        }

        [Test]
        public void NonFiniteOrDegenerateAim_IsRejected()
        {
            Assert.IsFalse(CombatAimRay.TryBuild(Vector3.zero, new Vector3(float.NaN, 0f, 1f), out _));
            Assert.IsFalse(CombatAimRay.TryBuild(Vector3.one, Vector3.one, out _));
        }
    }
}
