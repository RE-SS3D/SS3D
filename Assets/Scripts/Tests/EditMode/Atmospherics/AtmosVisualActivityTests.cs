using NUnit.Framework;
using SS3D.Systems.Atmospherics.Visualization;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    public class AtmosVisualActivityTests
    {
        private static readonly Vector4 Nothing = Vector4.zero;

        private static GasVisualProfileBuilder.GpuSet Profiles(int visibleChannel = -1) => new()
        {
            Scatter = new[] { Nothing, Nothing, visibleChannel == 2 ? new Vector4(0, 0, 0, 1) : Nothing, Nothing },
            Emission = new[] { Nothing, Nothing, Nothing, visibleChannel == 3 ? new Vector4(0, 0, 0, 1) : Nothing },
            Misc = new[] { Nothing, Nothing, Nothing, Nothing },
        };

        private static readonly Color32 Air = new(53, 202, 0, 0);

        [Test]
        public void RoomAir_IsCalm()
        {
            Assert.IsTrue(AtmosVisualActivity.IsCalm(new[] { 293.15f }, new[] { Air }, new[] { 0f }, Profiles()));
        }

        [Test]
        public void EmptyTile_IsCalm()
        {
            Assert.IsTrue(AtmosVisualActivity.IsCalm(new[] { 0f }, new[] { new Color32(0, 0, 0, 0) }, new[] { 0f }, Profiles()));
        }

        [Test]
        public void Fire_IsNotCalm()
        {
            Assert.IsFalse(AtmosVisualActivity.IsCalm(new[] { 293.15f }, new[] { Air }, new[] { 0.5f }, Profiles()));
        }

        [Test]
        public void AirWarmEnoughToShimmer_IsNotCalm()
        {
            Assert.IsFalse(AtmosVisualActivity.IsCalm(new[] { 294.15f }, new[] { Air }, new[] { 0f }, Profiles()));
        }

        [Test]
        public void TraceOfScatteringGas_IsNotCalm()
        {
            Color32 smoke = new(53, 201, 1, 0);
            Assert.IsFalse(AtmosVisualActivity.IsCalm(new[] { 293.15f }, new[] { smoke }, new[] { 0f }, Profiles(visibleChannel: 2)));
        }

        [Test]
        public void TraceOfEmissiveGas_IsNotCalm()
        {
            Color32 plasma = new(53, 201, 0, 1);
            Assert.IsFalse(AtmosVisualActivity.IsCalm(new[] { 293.15f }, new[] { plasma }, new[] { 0f }, Profiles(visibleChannel: 3)));
        }

        [Test]
        public void GasWithoutAVisualProfile_IsCalm()
        {
            Color32 inert = new(53, 201, 1, 0);
            Assert.IsTrue(AtmosVisualActivity.IsCalm(new[] { 293.15f }, new[] { inert }, new[] { 0f }, Profiles()));
        }

        [Test]
        public void UnknownProfiles_KeepThePassesRunning()
        {
            Assert.IsFalse(AtmosVisualActivity.IsCalm(new[] { 293.15f }, new[] { Air }, new[] { 0f }, default));
        }

        [Test]
        public void OneActiveTileAmongCalmOnes_IsNotCalm()
        {
            Assert.IsFalse(AtmosVisualActivity.IsCalm(new[] { 293.15f, 293.15f }, new[] { Air, Air }, new[] { 0f, 1f }, Profiles()));
        }
    }
}
