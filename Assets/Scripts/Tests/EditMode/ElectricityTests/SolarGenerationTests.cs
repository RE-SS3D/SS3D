using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Electricity;
using SS3D.Systems.Tile;
using UnityEngine;

namespace EditorTests
{
    [TestFixture]
    public class SolarGenerationTests
    {
        private const float Tolerance = 0.0001f;

        [SetUp]
        public void SetUp()
        {
            SolarCycle.ResetDefaults();
            SolarCycle.PeriodSeconds = 100f;
            SolarCycle.EclipseFraction = 0.2f;
        }

        [TearDown]
        public void TearDown()
        {
            SolarCycle.ResetDefaults();
        }

        [Test]
        public void SolarCycle_AzimuthSweepsFullCircle()
        {
            Assert.That(SolarCycle.GetSunAzimuthDegrees(0f), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(SolarCycle.GetSunAzimuthDegrees(25f), Is.EqualTo(90f).Within(Tolerance));
            Assert.That(SolarCycle.GetSunAzimuthDegrees(50f), Is.EqualTo(180f).Within(Tolerance));
            Assert.That(SolarCycle.GetSunAzimuthDegrees(100f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void SolarCycle_IntensityZeroDuringNightAndEclipse()
        {
            // Night half of cycle (phase 0.75 → t=75): sin negative → 0
            Assert.That(SolarCycle.GetSunIntensity(75f), Is.EqualTo(0f).Within(Tolerance));

            // Peak day (phase 0.25 → t=25) falls inside eclipse window (0.2 fraction → ±0.1)
            Assert.That(SolarCycle.GetSunIntensity(25f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void SolarCycle_IntensityPositiveOutsideEclipseOnDaySide()
        {
            // Phase 0.05 → still day, outside eclipse around 0.25
            float intensity = SolarCycle.GetSunIntensity(5f);
            Assert.That(intensity, Is.GreaterThan(0f));
            Assert.That(intensity, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void SolarCycle_NearestAimDirectionRoundsToEightWay()
        {
            Assert.AreEqual(Direction.North, SolarCycle.GetNearestAimDirection(0f));
            Assert.AreEqual(Direction.East, SolarCycle.GetNearestAimDirection(90f));
            Assert.AreEqual(Direction.South, SolarCycle.GetNearestAimDirection(180f));
            Assert.AreEqual(Direction.West, SolarCycle.GetNearestAimDirection(270f));
        }

        [Test]
        public void SolarCycle_AimFactorIsOneWhenAlignedAndZeroWhenOpposite()
        {
            Assert.That(SolarCycle.ComputeAimFactor(Direction.North, 0f), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(SolarCycle.ComputeAimFactor(Direction.South, 0f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void SolarPanel_ProducesNearPeakWhenAlignedAndSunlit()
        {
            // Day, outside eclipse: phase 0.05
            SolarCycle.TimeOverride = 5f;
            GameObject go = new("SolarPanelTest");
            try
            {
                SolarPanel panel = go.AddComponent<SolarPanel>();
                panel.PeakPowerKw = 2f;
                panel.SetAim(SolarCycle.GetNearestAimDirection(SolarCycle.GetSunAzimuthDegrees()));

                float expectedIntensity = SolarCycle.GetSunIntensity();
                float expectedAim = SolarCycle.ComputeAimFactor(panel.Aim, SolarCycle.GetSunAzimuthDegrees());
                Assert.That(panel.PowerProduction, Is.EqualTo(2f * expectedIntensity * expectedAim).Within(Tolerance));
                Assert.That(panel.PowerProduction, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SolarPanel_ProducesZeroWhenFacingAway()
        {
            SolarCycle.TimeOverride = 5f;
            GameObject go = new("SolarPanelTest");
            try
            {
                SolarPanel panel = go.AddComponent<SolarPanel>();
                panel.PeakPowerKw = 2f;
                Direction sunAim = SolarCycle.GetNearestAimDirection(SolarCycle.GetSunAzimuthDegrees());
                panel.SetAim(TileHelper.GetOpposite(sunAim));

                Assert.That(panel.PowerProduction, Is.EqualTo(0f).Within(0.05f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SolarPanel_ProducesZeroDuringEclipse()
        {
            SolarCycle.TimeOverride = 25f;
            GameObject go = new("SolarPanelTest");
            try
            {
                SolarPanel panel = go.AddComponent<SolarPanel>();
                panel.PeakPowerKw = 2f;
                panel.SetAim(Direction.North);

                Assert.That(SolarCycle.GetSunIntensity(), Is.EqualTo(0f).Within(Tolerance));
                Assert.That(panel.PowerProduction, Is.EqualTo(0f).Within(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SolarPanel_RotateAimStepAdvancesDirection()
        {
            GameObject go = new("SolarPanelTest");
            try
            {
                SolarPanel panel = go.AddComponent<SolarPanel>();
                panel.SetAim(Direction.North);
                panel.RotateAimStep();
                Assert.AreEqual(Direction.NorthEast, panel.Aim);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SolarTrackingBeacon_AimsNearbyPanelsTowardSun()
        {
            SolarCycle.TimeOverride = 50f; // azimuth 180 → South
            GameObject beaconGo = new("Beacon");
            GameObject nearGo = new("NearPanel");
            GameObject farGo = new("FarPanel");
            try
            {
                beaconGo.transform.position = Vector3.zero;
                nearGo.transform.position = new Vector3(2f, 0f, 0f);
                farGo.transform.position = new Vector3(50f, 0f, 0f);

                SolarTrackingBeacon beacon = beaconGo.AddComponent<SolarTrackingBeacon>();
                beacon.Radius = 8f;
                SolarPanel near = nearGo.AddComponent<SolarPanel>();
                SolarPanel far = farGo.AddComponent<SolarPanel>();
                near.SetAim(Direction.North);
                far.SetAim(Direction.North);
                // Edit mode runs no OnEnable, which is where panels register themselves in play.
                SceneRegistry<SolarPanel>.Register(near);
                SceneRegistry<SolarPanel>.Register(far);

                int aimed = beacon.AimNearbyPanels();

                Assert.That(aimed, Is.EqualTo(1));
                Assert.AreEqual(Direction.South, near.Aim);
                Assert.AreEqual(Direction.North, far.Aim);
            }
            finally
            {
                SceneRegistry<SolarPanel>.Unregister(nearGo.GetComponent<SolarPanel>());
                SceneRegistry<SolarPanel>.Unregister(farGo.GetComponent<SolarPanel>());
                Object.DestroyImmediate(beaconGo);
                Object.DestroyImmediate(nearGo);
                Object.DestroyImmediate(farGo);
            }
        }
    }
}
