using System;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Area;
using SS3D.Systems.Audio;
using SS3D.Systems.Electricity;

namespace EditorTests
{
    public class AmbienceProfileTests
    {
        private static AmbienceProfileCatalog Catalog => AmbienceProfileCatalog.Instance;

        [Test]
        public void GenericProfileExistsAndHasABed()
        {
            Assert.IsTrue(Catalog.TryGet(AmbienceProfileCatalog.GenericProfileId, out AmbienceProfile profile));
            Assert.IsNotNull(profile.Bed);
        }

        [TestCase("no-such-profile")]
        [TestCase("")]
        [TestCase(null)]
        public void UnknownIdResolvesToTheGenericProfile(string id)
        {
            Assert.AreEqual(AmbienceProfileCatalog.GenericProfileId, Catalog.Resolve(id).Id);
        }

        [Test]
        public void LookupIgnoresCase()
        {
            Assert.IsTrue(Catalog.TryGet("STATION", out _));
        }

        [Test]
        public void EveryProfileHasAUniqueLowerCaseId()
        {
            string[] ids = Catalog.ProfileIds.ToArray();
            CollectionAssert.AllItemsAreUnique(ids);
            Assert.IsTrue(ids.All(id => id == id.ToLowerInvariant()), string.Join(", ", ids));
        }

        [Test]
        public void OverrideBeatsDepartment()
        {
            AreaRecord room = new() { ParentTag = "Medical", AmbienceProfileId = "morgue" };
            Assert.AreEqual("morgue", room.EffectiveAmbienceProfileId);
        }

        [Test]
        public void DepartmentNamesTheProfileWhenThereIsNoOverride()
        {
            AreaRecord room = new() { ParentTag = "Medical" };
            Assert.AreEqual("medical", room.EffectiveAmbienceProfileId);
        }

        [Test]
        public void RoomWithNoDepartmentAsksForNothing()
        {
            Assert.AreEqual(string.Empty, new AreaRecord().EffectiveAmbienceProfileId);
        }

        [TestCase("engineering")]
        [TestCase("medical")]
        [TestCase("maintenance")]
        [TestCase("morgue")]
        public void ThemedProfileExists(string id)
        {
            Assert.IsTrue(Catalog.TryGet(id, out _));
        }

        [Test]
        public void EveryProfileHasItsClips()
        {
            foreach (string id in Catalog.ProfileIds)
            {
                Catalog.TryGet(id, out AmbienceProfile profile);
                Assert.IsNotNull(profile.Bed, id);
                Assert.IsNotEmpty(profile.Occasional, id);
                Assert.IsTrue(profile.Occasional.All(clip => clip != null), id);
            }

            Assert.IsNotNull(Catalog.AirBed);
        }

        [Test]
        public void CatalogHasEveryWindLayerAndASpaceBed()
        {
            Assert.AreEqual(AudioAirState.WindLayerCount, Catalog.WindLayers.Length);
            Assert.IsTrue(Catalog.WindLayers.All(clip => clip != null));
            Assert.IsNotNull(Catalog.SpaceBed);
        }

        [Test]
        public void ScheduledGapIsLongAndTheFirstSoundWaitsForIt()
        {
            AmbienceScheduler scheduler = new(new Random(1));

            Assert.IsFalse(scheduler.TryPick(0f, 3, out _));
            Assert.IsFalse(scheduler.TryPick(AmbienceScheduler.MinGap - 0.1f, 3, out _));
            Assert.IsTrue(scheduler.TryPick(AmbienceScheduler.MaxGap, 3, out _));
        }

        [Test]
        public void NoProfileClipsMeansNothingIsDue()
        {
            Assert.IsFalse(new AmbienceScheduler(new Random(1)).TryPick(1000f, 0, out _));
        }

        [Test]
        public void SameClipNeverPlaysTwiceInARow()
        {
            AmbienceScheduler scheduler = new(new Random(7));
            int last = -1;
            float now = 0f;
            for (int i = 0; i < 200; i++)
            {
                now += AmbienceScheduler.MaxGap * 2f;
                scheduler.TryPick(now, 3, out int index);
                if (index < 0) continue;
                Assert.AreNotEqual(last, index);
                last = index;
                scheduler.ScheduleAfter(now);
            }
        }

        [TestCase(ApcControlFlags.All, true, 5f, true)]
        [TestCase(ApcControlFlags.Lighting, true, 5f, false)]
        [TestCase(ApcControlFlags.All, false, 5f, false)]
        [TestCase(ApcControlFlags.All, true, 0f, false)]
        public void EquipmentPowerNeedsTheChannelAndTheGrid(ApcControlFlags channels, bool gridMeetsLoad, float supply, bool expected)
        {
            CircuitStats stats = new() { GridMeetsLoad = gridMeetsLoad, TotalSupplyKw = supply, ApcBatteryCharge = 10f };
            Assert.AreEqual(expected, AreaLightingStateDeriver.DeriveEquipmentPowered(stats, channels));
        }
    }
}
