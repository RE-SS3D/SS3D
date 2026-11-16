using NUnit.Framework;
using SS3D.Systems.Settings;

namespace EditorTests
{
    public class PlayerDisplayPlanTests
    {
        [TestCase(true, 0, 1, -1)]
        [TestCase(true, 144, 1, -1)]
        [TestCase(false, 0, 0, -1)]
        [TestCase(false, 60, 0, 60)]
        public void VSyncWinsOverTheFrameCap(bool vSync, int frameCap, int vSyncCount, int targetFrameRate)
        {
            FramePacing pacing = PlayerDisplayPlan.Pace(vSync, frameCap);

            Assert.That((pacing.VSyncCount, pacing.TargetFrameRate), Is.EqualTo((vSyncCount, targetFrameRate)));
        }

        [Test]
        public void FullRenderScaleUsesNoUpscalerAndLeavesAntiAliasingFree()
        {
            PlayerDisplaySettings display = new() { RenderScale = 1f, Upscaler = PlayerUpscaler.Stp, AntiAliasing = PlayerAntiAliasing.Fxaa };

            RenderPlan plan = PlayerDisplayPlan.Resolve(display, stpSupported: true);

            Assert.That(plan.Upscaler, Is.Null);
            Assert.That(plan.AntiAliasing, Is.EqualTo(PlayerAntiAliasing.Fxaa));
            Assert.That(plan.AntiAliasingLocked, Is.False);
        }

        [Test]
        public void StpBelowFullScaleForcesTaaAndLocksTheChoice()
        {
            PlayerDisplaySettings display = new() { RenderScale = 0.67f, Upscaler = PlayerUpscaler.Stp, AntiAliasing = PlayerAntiAliasing.Smaa };

            RenderPlan plan = PlayerDisplayPlan.Resolve(display, stpSupported: true);

            Assert.That(plan.RenderScale, Is.EqualTo(0.67f));
            Assert.That(plan.Upscaler, Is.EqualTo(PlayerUpscaler.Stp));
            Assert.That(plan.AntiAliasing, Is.EqualTo(PlayerAntiAliasing.Taa));
            Assert.That(plan.AntiAliasingLocked, Is.True);
        }

        [Test]
        public void StpFallsBackToFsr1WhenUnsupported()
        {
            PlayerDisplaySettings display = new() { RenderScale = 0.5f, Upscaler = PlayerUpscaler.Stp, AntiAliasing = PlayerAntiAliasing.Smaa };

            RenderPlan plan = PlayerDisplayPlan.Resolve(display, stpSupported: false);

            Assert.That(plan.Upscaler, Is.EqualTo(PlayerUpscaler.Fsr1));
            Assert.That(plan.AntiAliasing, Is.EqualTo(PlayerAntiAliasing.Smaa));
            Assert.That(plan.AntiAliasingLocked, Is.False);
        }

        [TestCase(PlayerUpscaler.Fsr1)]
        [TestCase(PlayerUpscaler.Bilinear)]
        public void OtherUpscalersKeepTheChosenAntiAliasing(PlayerUpscaler upscaler)
        {
            PlayerDisplaySettings display = new() { RenderScale = 0.77f, Upscaler = upscaler, AntiAliasing = PlayerAntiAliasing.Off };

            RenderPlan plan = PlayerDisplayPlan.Resolve(display, stpSupported: true);

            Assert.That(plan.Upscaler, Is.EqualTo(upscaler));
            Assert.That(plan.AntiAliasing, Is.EqualTo(PlayerAntiAliasing.Off));
        }
    }
}
