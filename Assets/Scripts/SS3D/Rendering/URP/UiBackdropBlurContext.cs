namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Dual Kawase fullscreen blur strength (0..1) for diegetic UI focus.
    /// Driven by <c>ScreenEffectsSubSystem.SetUiBackdropBlur</c>.
    /// </summary>
    public static class UiBackdropBlurContext
    {
        public static float Intensity { get; set; }
    }
}
