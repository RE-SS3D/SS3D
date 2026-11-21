using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell
{
    /// <summary>
    /// Draws a full-screen surface at the UI bible's reference size. The shell's panel is constant pixel size (1 px per
    /// screen px at 96 dpi, times the player's UI scale), so a screen drawn in the bible on its 1200 × 675 stage would
    /// come out at 1/1.6 of its drawing at 1080p if its USS numbers were used as they are. Instead the surface is laid
    /// out at the reference size (wider or taller to match the host's shape) and scaled up to fill its host, then by the
    /// player's UI scale, so it draws as the bible does at any resolution.
    /// </summary>
    public static class UiReferenceScale
    {
        public const float Width = 1200f;
        public const float Height = 675f;

        /// <summary>Panel px per reference px for a host of <paramref name="hostSize"/> panel px.</summary>
        /// <param name="userScale">The player's UI scale (the shell PanelSettings' scale); 1 leaves the fit as is.</param>
        public static float Factor(Vector2 hostSize, float userScale) =>
            Mathf.Max(0.01f, Mathf.Min(hostSize.x / Width, hostSize.y / Height) * Mathf.Max(0.01f, userScale));

        /// <summary>
        /// Lays <paramref name="surface"/> out at the reference size and scales it to fill <paramref name="host"/>,
        /// again whenever the host resizes (a new resolution, or a new UI scale, which resizes the panel).
        /// </summary>
        /// <returns>Stops following the host; call it when the surface leaves a host that outlives it.</returns>
        public static Action Fit(VisualElement host, VisualElement surface, Func<float> userScale)
        {
            surface.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0), 0);
            EventCallback<GeometryChangedEvent> refit = _ => Apply(host, surface, userScale?.Invoke() ?? 1f);
            host.RegisterCallback(refit);
            Apply(host, surface, userScale?.Invoke() ?? 1f);
            return () => host.UnregisterCallback(refit);
        }

        public static void Apply(VisualElement host, VisualElement surface, float userScale)
        {
            Vector2 size = host.layout.size;
            if (size.x <= 0f || size.y <= 0f || float.IsNaN(size.x) || float.IsNaN(size.y)) return;
            float factor = Factor(size, userScale);
            surface.style.position = Position.Absolute;
            surface.style.left = 0;
            surface.style.top = 0;
            surface.style.right = StyleKeyword.Auto;
            surface.style.bottom = StyleKeyword.Auto;
            surface.style.width = size.x / factor;
            surface.style.height = size.y / factor;
            surface.style.scale = new Scale(new Vector2(factor, factor));
        }
    }
}
