using UnityEngine;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Bracket / label tint for the zone reticle. Exactly one mode applies per frame;
    /// compose priority is Recharging > Valid > Idle.
    /// </summary>
    public enum ZoneReticleColorMode
    {
        Idle = 0,
        Valid = 1,
        Recharging = 2,
    }

    /// <summary>
    /// Immutable per-frame snapshot for <see cref="ZoneTargetReticle.Apply"/>.
    /// Produced only by <see cref="ZoneReticleDriver.Tick"/>.
    /// </summary>
    public readonly struct ZoneReticleFrame
    {
        public readonly bool Visible;
        public readonly Vector2 CursorScreen;
        public readonly string ZoneLabel;
        public readonly ZoneReticleColorMode Color;
        public readonly float BracketReady01;
        public readonly float CrossFlashT;
        public readonly Vector2 ShakeOffset;
        /// <summary>0 = tight aim, 1 = max bloom (ranged accuracy cone feedback).</summary>
        public readonly float Bloom01;

        public ZoneReticleFrame(
            bool visible,
            Vector2 cursorScreen,
            string zoneLabel,
            ZoneReticleColorMode color,
            float bracketReady01,
            float crossFlashT,
            Vector2 shakeOffset,
            float bloom01 = 0f)
        {
            Visible = visible;
            CursorScreen = cursorScreen;
            ZoneLabel = zoneLabel ?? string.Empty;
            Color = color;
            BracketReady01 = bracketReady01;
            CrossFlashT = crossFlashT;
            ShakeOffset = shakeOffset;
            Bloom01 = Mathf.Clamp01(bloom01);
        }
    }
}
