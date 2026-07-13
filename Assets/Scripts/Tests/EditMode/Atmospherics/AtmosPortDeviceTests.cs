using NUnit.Framework;
using SS3D.Systems.Atmospherics.Pipes;

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
    }
}

