namespace SS3D.Systems.Settings
{
    public readonly struct FramePacing
    {
        public readonly int VSyncCount;
        public readonly int TargetFrameRate;

        public FramePacing(int vSyncCount, int targetFrameRate)
        {
            VSyncCount = vSyncCount;
            TargetFrameRate = targetFrameRate;
        }
    }

    public readonly struct RenderPlan
    {
        public readonly float RenderScale;
        public readonly PlayerUpscaler? Upscaler;
        public readonly PlayerAntiAliasing AntiAliasing;
        public readonly bool AntiAliasingLocked;

        public RenderPlan(float renderScale, PlayerUpscaler? upscaler, PlayerAntiAliasing antiAliasing, bool locked)
        {
            RenderScale = renderScale;
            Upscaler = upscaler;
            AntiAliasing = antiAliasing;
            AntiAliasingLocked = locked;
        }
    }

    /// <summary>Pure rules from Display settings to engine values (Decisions 5–7), testable without a GPU.</summary>
    public static class PlayerDisplayPlan
    {
        /// <summary>VSync on ignores the cap; Unity's targetFrameRate -1 means uncapped.</summary>
        public static FramePacing Pace(bool vSync, int frameCap) =>
            vSync ? new FramePacing(1, -1) : new FramePacing(0, frameCap > 0 ? frameCap : -1);

        /// <summary>
        /// The upscaler only runs below 100 %. STP needs SM5 compute and falls back to FSR 1; while STP runs it forces
        /// TAA, so the AA choice is shown locked.
        /// </summary>
        public static RenderPlan Resolve(PlayerDisplaySettings display, bool stpSupported)
        {
            if (display.RenderScale >= 1f) return new RenderPlan(1f, null, display.AntiAliasing, false);

            PlayerUpscaler upscaler = display.Upscaler == PlayerUpscaler.Stp && !stpSupported ? PlayerUpscaler.Fsr1 : display.Upscaler;
            bool stp = upscaler == PlayerUpscaler.Stp;
            return new RenderPlan(display.RenderScale, upscaler, stp ? PlayerAntiAliasing.Taa : display.AntiAliasing, stp);
        }
    }
}
