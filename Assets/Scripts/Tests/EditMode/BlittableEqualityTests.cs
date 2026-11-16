using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Health;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Tech-debt P6.2: per-tick SyncVar structs compare without boxing, and still see every field change (a missed field
    /// would stop that change from syncing).
    /// </summary>
    public sealed class BlittableEqualityTests
    {
        [Test]
        public void SyncedHealthStructs_UseTheirOwnEquality()
        {
            // ObjectEqualityComparer is what boxes; a struct implementing IEquatable gets the generic one.
            StringAssert.DoesNotContain("ObjectEqualityComparer", EqualityComparer<HealthSnapshot>.Default.GetType().Name);
            StringAssert.DoesNotContain("ObjectEqualityComparer", EqualityComparer<HealthDebugDetail>.Default.GetType().Name);
        }

        [Test]
        public void HealthSnapshot_EqualCopies_AndEveryKindOfFieldCounts()
        {
            HealthSnapshot a = HealthSnapshot.Default;
            HealthSnapshot b = HealthSnapshot.Default;
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            b.WorstZoneBurn += 1f;
            Assert.IsFalse(a.Equals(b), "a float field");

            b = a;
            b.CanUseArms = !b.CanUseArms;
            Assert.IsFalse(a.Equals(b), "a bool field");

            b = a;
            b.Environment.PressureKpa += 1f;
            Assert.IsFalse(a.Equals(b), "a field of a nested struct");

            b = a;
            b.Pools.OxyDebt += 1f;
            Assert.IsFalse(a.Equals(b), "the systemic pools");
        }

        [Test]
        public void HealthDebugDetail_SeesZoneAndOrganChanges()
        {
            var a = new HealthDebugDetail();
            HealthDebugDetail b = a;
            Assert.IsTrue(a.Equals(b));

            b.Groin.Brute = 3f;
            Assert.IsFalse(a.Equals(b));

            b = a;
            b.Liver.FunctionPercent = 50f;
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void IdenticalNaNs_AreEqual_SoAStuckNaNDoesNotResyncEveryTick()
        {
            HealthSnapshot a = HealthSnapshot.Default;
            a.TotalBleedingRate = float.NaN;
            HealthSnapshot b = a;
            Assert.IsTrue(BlittableEquality.Equal(a, b));
        }
    }
}
