namespace SS3D.Systems.ScreenEffects
{
    /// <summary>
    /// Sustained screen-space feedback states, each driven by an intensity in the 0..1 range.
    /// See the "Screen-Space Effect" sections of the main HUD design doc (§5) for the reference behaviour.
    /// Momentary events (e.g. a melee hit flash) are not part of this enum - see <see cref="ScreenEffectsSubSystem.TriggerHitFlash"/>.
    /// </summary>
    public enum ScreenEffectType
    {
        HotRoom,
        OnFire,
        ColdRoom,
        Freezing,
        LowOxygen,
        DyingCritical,
        BloodLossTunnelVision,
        Concussion,
        Unconscious,
    }
}
