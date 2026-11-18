using FishNet.Serializing;
using NUnit.Framework;
using SS3D.Systems.Health;
using SS3D.Tests;
using SS3D.UI.MachineInterface;
using System.Collections.Generic;

namespace EditorTests
{
    public class HealthScanSnapshotTests : EditModeTest
    {
        private static readonly float[] HealthyOrgans = { 100f, 100f, 100f, 100f, 100f };

        [Test]
        public void HealthyPatient_ReadsClear()
        {
            HealthScanViewModel model = HealthScanSnapshotMapper.ToViewModel(Scan(HealthSnapshot.Default, Zones(), HealthyOrgans));

            Assert.AreEqual(StatusTone.Success, model.StatusTone);
            Assert.AreEqual("Stable", model.StatusText);
            Assert.AreEqual(HealthScanRhythm.Steady, model.Rhythm);
            CollectionAssert.IsEmpty(model.WoundZones);
            Assert.AreEqual("100%", model.BloodText);
            Assert.AreEqual("100%", model.SpO2Text);
            Assert.AreEqual(StatusTone.Success, model.Organs[(int)OrganType.Liver].Tone);
            Assert.AreEqual(HealthScanSnapshotMapper.ZoneFill(0f, 0f), model.Zones[(int)BodyZone.Chest].Fill);
        }

        [Test]
        public void Builder_ReadsZonesAndOrgansOffTheBody()
        {
            ZoneDamageState[] zones = Zones();
            zones[(int)BodyZone.LeftArm] = new ZoneDamageState { Brute = 80f, Severity = WoundSeverity.Disabled, IsDisabled = true, BleedingRate = 1f };
            zones[(int)BodyZone.RightLeg] = new ZoneDamageState { Burn = 30f, IsSplinted = true, IsDisabled = true };
            float[] organs = { 100f, 60f, 20f, 90f, 100f };

            HealthScanSnapshot scan = Scan(HealthSnapshot.Default, zones, organs);

            Assert.AreEqual(80f, scan.ZoneBrute[(int)BodyZone.LeftArm]);
            Assert.AreEqual(30f, scan.ZoneBurn[(int)BodyZone.RightLeg]);
            Assert.AreEqual(HealthScanZoneFlags.Bleeding | HealthScanZoneFlags.Disabled, scan.ZoneFlags[(int)BodyZone.LeftArm]);
            Assert.AreEqual(HealthScanZoneFlags.Disabled | HealthScanZoneFlags.Splinted, scan.ZoneFlags[(int)BodyZone.RightLeg]);
            Assert.AreEqual(20f, scan.OrganFunction[(int)OrganType.LeftLung]);
            Assert.AreEqual(WoundSeverity.Disabled, scan.ZoneSeverity[(int)BodyZone.LeftArm]);
            Assert.AreEqual("Ada", scan.SubjectName);
        }

        [Test]
        public void Mapper_FlagsInjuriesAndOrgansOnTheSimulationsThresholds()
        {
            ZoneDamageState[] zones = Zones();
            zones[(int)BodyZone.Chest] = new ZoneDamageState { Brute = 78f, Severity = WoundSeverity.Severe, BleedingRate = 0.5f };
            float[] organs = { 100f, 60f, 20f, 90f, 100f };

            HealthScanViewModel model = HealthScanSnapshotMapper.ToViewModel(Scan(HealthSnapshot.Default, zones, organs));

            Assert.AreEqual(StatusTone.Warning, model.StatusTone);
            StringAssert.StartsWith("Injured", model.StatusText);
            Assert.AreEqual(StatusTone.Warning, model.Organs[(int)OrganType.Heart].Tone);
            Assert.AreEqual(StatusTone.Danger, model.Organs[(int)OrganType.LeftLung].Tone);
            Assert.AreEqual(StatusTone.Success, model.Organs[(int)OrganType.RightLung].Tone);
            Assert.AreEqual(model.Organs[(int)OrganType.LeftLung].Fill, model.Lungs.Fill, "the lungs drawing takes the worse lung");
            Assert.AreEqual("Severe", model.Zones[(int)BodyZone.Chest].TierLabel);
            StringAssert.Contains("BLEEDING", model.Zones[(int)BodyZone.Chest].FlagsText);
            CollectionAssert.AreEqual(new[] { (int)BodyZone.Chest }, model.WoundZones);
            Assert.Greater(model.Zones[(int)BodyZone.Chest].BoneOpacity, 0f, "bone shows through at high brute");
        }

        [TestCase(1f, StatusTone.Neutral)]
        [TestCase(0.7f, StatusTone.Warning)]
        [TestCase(0.4f, StatusTone.Danger)]
        public void BloodTone_FollowsLoss(float ratio, StatusTone expected)
        {
            HealthSnapshot vitals = HealthSnapshot.Default;
            vitals.Pools.BloodVolumeRatio = ratio;

            Assert.AreEqual(expected, HealthScanSnapshotMapper.ToViewModel(Scan(vitals, Zones(), HealthyOrgans)).BloodTone);
        }

        [Test]
        public void ZoneFill_TakesTheLeadingDamageColour()
        {
            UnityEngine.Color brute = HealthScanSnapshotMapper.ZoneFill(60f, 10f);
            UnityEngine.Color burn = HealthScanSnapshotMapper.ZoneFill(10f, 60f);

            Assert.Greater(brute.r - brute.g, burn.r - burn.g, "brute reads redder than burn");
        }

        [Test]
        public void CardiacArrest_FlatLinesTheTrace()
        {
            HealthSnapshot vitals = HealthSnapshot.Default;
            vitals.State = HealthState.CardiacArrest;
            vitals.IsCardiacArrest = true;

            HealthScanViewModel model = HealthScanSnapshotMapper.ToViewModel(Scan(vitals, Zones(), HealthyOrgans));

            Assert.AreEqual(StatusTone.Danger, model.StatusTone);
            StringAssert.StartsWith("Cardiac Arrest", model.StatusText);
            Assert.AreEqual(HealthScanRhythm.Arrest, model.Rhythm);
        }

        [Test]
        public void DisabledLimb_DrawsBlackWithARim_AndSeveredOnlyAsAGhost()
        {
            ZoneDamageState[] zones = Zones();
            zones[(int)BodyZone.LeftArm] = new ZoneDamageState { Brute = 90f, Severity = WoundSeverity.Disabled, IsDisabled = true };
            zones[(int)BodyZone.RightLeg] = new ZoneDamageState { Brute = 100f, Severity = WoundSeverity.Severed, IsSevered = true };

            HealthScanViewModel model = HealthScanSnapshotMapper.ToViewModel(Scan(HealthSnapshot.Default, zones, HealthyOrgans));

            Assert.AreEqual(HealthScanSnapshotMapper.DisabledFill, model.Zones[(int)BodyZone.LeftArm].Fill);
            Assert.Greater(model.Zones[(int)BodyZone.LeftArm].Glow.a, 0f);
            Assert.AreEqual(WoundSeverity.Severed, model.Zones[(int)BodyZone.RightLeg].Tier);
            Assert.Less(model.Zones[(int)BodyZone.RightLeg].Opacity, 1f);
            Assert.AreEqual(0f, model.Zones[(int)BodyZone.RightLeg].BoneOpacity);
        }

        [Test]
        public void Shock_MarksTheGamesThresholdsAndPendingPain()
        {
            HealthSnapshot vitals = HealthSnapshot.Default;
            vitals.Shock = 62f;
            vitals.IncomingPain = 9f;
            vitals.TotalBleedingRate = 1.5f;

            HealthScanViewModel model = HealthScanSnapshotMapper.ToViewModel(Scan(vitals, Zones(), HealthyOrgans));

            Assert.AreEqual(StatusTone.Warning, model.ShockTone);
            Assert.AreEqual("62  (+9 incoming)", model.ShockText);
            Assert.AreEqual(HealthConstants.ShockDownedThreshold / HealthScanSnapshotMapper.ShockBarMax, model.DownedMarker, 1e-5f);
            Assert.Greater(model.IncomingFraction, 0f);
            Assert.AreEqual(HealthScanRhythm.Rapid, model.Rhythm);
            Assert.AreEqual(StatusTone.Danger, model.BleedTone);
            StringAssert.EndsWith("%/MIN", model.BleedText);
        }

        [Test]
        public void Snapshot_RoundTrips()
        {
            ZoneDamageState[] zones = Zones();
            zones[(int)BodyZone.Head] = new ZoneDamageState { Brute = 12.5f, Burn = 3f, IsSevered = true };
            HealthSnapshot vitals = HealthSnapshot.Default;
            vitals.State = HealthState.Downed;
            vitals.Shock = 72f;
            vitals.IncomingPain = 4f;
            vitals.TotalBleedingRate = 0.5f;
            vitals.Pools.OxyDebt = 0.3f;
            vitals.CriticalFlags = HealthCriticalFlags.HighShock;
            HealthScanSnapshot original = Scan(vitals, zones, new[] { 90f, 80f, 70f, 60f, 50f });
            original.InterfaceId = MachineInterfaceIds.HealthScanner;
            original.DeviceTitle = "VSU-5R";

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteHealthScanSnapshot(original);
            using PooledReader reader = ReaderPool.Retrieve(writer.GetArraySegment(), null);
            HealthScanSnapshot read = reader.ReadHealthScanSnapshot();

            Assert.AreEqual(original.InterfaceId, read.InterfaceId);
            Assert.AreEqual(original.SubjectName, read.SubjectName);
            Assert.AreEqual(HealthState.Downed, read.State);
            Assert.AreEqual(HealthCriticalFlags.HighShock, read.CriticalFlags);
            Assert.AreEqual(72f, read.Shock);
            Assert.AreEqual(0.3f, read.OxyDebt);
            CollectionAssert.AreEqual(original.ZoneBrute, read.ZoneBrute);
            CollectionAssert.AreEqual(original.ZoneBurn, read.ZoneBurn);
            CollectionAssert.AreEqual(original.ZoneFlags, read.ZoneFlags);
            CollectionAssert.AreEqual(original.OrganFunction, read.OrganFunction);
            CollectionAssert.AreEqual(original.ZoneSeverity, read.ZoneSeverity);
            Assert.AreEqual(4f, read.IncomingPain);
            Assert.AreEqual(0.5f, read.TotalBleedingRate);
        }

        [Test]
        public void Preview_BuildsBothScenarios()
        {
            Assert.AreEqual("Stable", HealthScanViewModel.CreatePreview(false).StatusText);
            Assert.AreEqual(StatusTone.Danger, HealthScanViewModel.CreatePreview(true).StatusTone);
        }

        private static HealthScanSnapshot Scan(HealthSnapshot vitals, ZoneDamageState[] zones, float[] organs)
        {
            HealthDebugDetail detail = HealthDebugDetail.FromStates(zones, new List<OrganState>());
            return HealthScanSnapshotBuilder.Build(vitals, detail, organs, "Ada");
        }

        private static ZoneDamageState[] Zones()
        {
            var zones = new ZoneDamageState[HealthConstants.ZoneCount];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ZoneDamageState.Default;
            }

            return zones;
        }
    }
}
