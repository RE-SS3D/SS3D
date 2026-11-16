using NUnit.Framework;
using SS3D.Systems.Area;
using SS3D.Systems.Electricity;

namespace EditorTests
{
    public class AreaLightFixturePolicyTests
    {
        [Test]
        public void WithoutArea_NeverEmits()
        {
            Assert.IsFalse(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: false,
                AreaLightingState.Dark,
                LightFixtureCapability.NormalOnly,
                PowerStatus.Powered,
                out bool emergencyVisuals));
            Assert.IsFalse(emergencyVisuals);

            Assert.IsFalse(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: false,
                AreaLightingState.Normal,
                LightFixtureCapability.EmergencyCapable,
                PowerStatus.Inactive,
                out _));
        }

        [Test]
        public void NormalArea_RequiresPoweredConsumer()
        {
            Assert.IsTrue(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: true,
                AreaLightingState.Normal,
                LightFixtureCapability.NormalOnly,
                PowerStatus.Powered,
                out bool emergencyVisuals));
            Assert.IsFalse(emergencyVisuals);

            Assert.IsFalse(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: true,
                AreaLightingState.Normal,
                LightFixtureCapability.EmergencyCapable,
                PowerStatus.Inactive,
                out _));
        }

        [Test]
        public void EmergencyArea_RequiresPoweredEmergencyCapableFixture()
        {
            Assert.IsTrue(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: true,
                AreaLightingState.Emergency,
                LightFixtureCapability.EmergencyCapable,
                PowerStatus.Powered,
                out bool emergencyVisuals));
            Assert.IsTrue(emergencyVisuals);

            Assert.IsFalse(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: true,
                AreaLightingState.Emergency,
                LightFixtureCapability.EmergencyCapable,
                PowerStatus.Inactive,
                out _));
        }

        [Test]
        public void EmergencyArea_OnlyEmergencyCapableFixturesEmit()
        {
            Assert.IsFalse(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: true,
                AreaLightingState.Emergency,
                LightFixtureCapability.NormalOnly,
                PowerStatus.Powered,
                out _));
        }

        [Test]
        public void DarkArea_NeverEmits()
        {
            Assert.IsFalse(AreaLightFixturePolicy.ShouldEmitLight(
                hasArea: true,
                AreaLightingState.Dark,
                LightFixtureCapability.EmergencyCapable,
                PowerStatus.Powered,
                out _));
        }
    }
}
