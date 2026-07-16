using NUnit.Framework;
using SS3D.Systems.Atmospherics.Pipes;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    public sealed class AtmosPortDeviceTests
    {
        [TestCase(0f, 0, false)]
        [TestCase(50f, 0, false)]
        [TestCase(101f, 101, false)]
        [TestCase(101.0f, 102, true)]
        [TestCase(150f, 101, false)]
        public void Vent_ShouldVentToTurf_RespectsTargetPressure(float turfPressureKpa, int targetKpa, bool expected)
        {
            Assert.AreEqual(expected, VentController.ShouldVentToTurf(turfPressureKpa, targetKpa));
        }

        [TestCase(0f, 0, false)]
        [TestCase(101.3f, 0, false)]
        [TestCase(4487f, 4500, true)]
        [TestCase(4500f, 4500, false)]
        [TestCase(6218f, 4500, false)]
        public void Pump_ShouldPumpToOutlet_RespectsTargetPressure(float outletPressureKpa, int targetKpa, bool expected)
        {
            Assert.AreEqual(expected, AtmosPumpController.ShouldPumpToOutlet(outletPressureKpa, targetKpa));
        }

        [Test]
        public void Pump_ComputePumpFlow_StallsAboveMaxDifferential()
        {
            float budget = AtmosPortFlow.ComputePumpFlowMoles(
                101.3f,
                10_000f,
                AtmosPortConstants.PumpRatedFlowMolesPerSecond,
                AtmosPortConstants.PumpMaxDifferentialKpa,
                1f,
                out _,
                out bool stalled);

            Assert.IsTrue(stalled);
            Assert.AreEqual(0f, budget);
        }

        [TestCase(1, 4f)]
        [TestCase(5, 20f)]
        [TestCase(10, 40f)]
        public void Scrubber_GetRatedFlowMolesPerSecond_ScalesWithFlowRate(int flowRate, float expected)
        {
            Assert.AreEqual(expected, ScrubberController.GetRatedFlowMolesPerSecond(flowRate), 0.001f);
        }

        [TestCase(0, 4f)]
        [TestCase(11, 40f)]
        public void Scrubber_GetRatedFlowMolesPerSecond_ClampsFlowRate(int flowRate, float expected)
        {
            Assert.AreEqual(expected, ScrubberController.GetRatedFlowMolesPerSecond(flowRate), 0.001f);
        }
    }
}
