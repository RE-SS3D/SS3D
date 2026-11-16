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

        [TestCase(500f, 101.3f, 1f)]
        [TestCase(101.3f, 101.3f, 1f)]
        [TestCase(0f, 0f, 1f)]
        [TestCase(101.3f, 326.3f, 0.5f)]
        [TestCase(101.3f, 461.3f, 0.2f)]
        public void Pump_ComputePumpFlow_RunsAtRatedFlowThenTapersWithBackpressure(float inletKpa, float outletKpa, float expectedFactor)
        {
            float budget = AtmosPortFlow.ComputePumpFlowMoles(
                inletKpa,
                outletKpa,
                100f,
                450f,
                0.5f,
                out float differentialKpa,
                out bool stalled);

            Assert.IsFalse(stalled);
            Assert.AreEqual(50f * expectedFactor, budget, 0.01f);
            Assert.AreEqual(inletKpa - outletKpa, differentialKpa, 0.001f);
        }

        [Test]
        public void Pump_ComputePumpFlow_StallsAtMaxDifferential()
        {
            float budget = AtmosPortFlow.ComputePumpFlowMoles(100f, 550f, 100f, 450f, 1f, out _, out bool stalled);

            Assert.IsTrue(stalled);
            Assert.AreEqual(0f, budget);
        }

        [Test]
        public void Pump_ComputePumpFlow_NeverStallsOnForwardPressure()
        {
            float budget = AtmosPortFlow.ComputePumpFlowMoles(5000f, 100f, 100f, 450f, 1f, out _, out bool stalled);

            Assert.IsFalse(stalled);
            Assert.AreEqual(100f, budget, 0.001f);
        }

        [Test]
        public void MolesForPressure_InvertsNetworkPressure()
        {
            var moles = new float[1];
            moles[0] = AtmosPipeThermo.GetMolesForPressure(250f, 293f, 5f);

            Assert.AreEqual(250f, AtmosPipeThermo.GetPressure(moles, 293f, 5f, 1), 0.01f);
        }

        [TestCase(1, 6f)]
        [TestCase(5, 30f)]
        [TestCase(10, 60f)]
        public void Scrubber_GetRatedFlowMolesPerSecond_ScalesWithFlowRate(int flowRate, float expected)
        {
            Assert.AreEqual(expected, ScrubberController.GetRatedFlowMolesPerSecond(flowRate), 0.001f);
        }

        [TestCase(0, 6f)]
        [TestCase(11, 60f)]
        public void Scrubber_GetRatedFlowMolesPerSecond_ClampsFlowRate(int flowRate, float expected)
        {
            Assert.AreEqual(expected, ScrubberController.GetRatedFlowMolesPerSecond(flowRate), 0.001f);
        }
    }
}
