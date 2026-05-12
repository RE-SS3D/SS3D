using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data
{
    public enum PaletteColor
    {
        White,
        TextPrimary,
        TextDisabled,
        UiOverlay,
        UiOverlayHighlighted,
        UiOverlayDisabled,
        LightGreen,
        NormalGreen,
        LightBlue,
        LightRed,
        LogBlue,
        LogGreen,
        LogRed,
        LogServer,
        LogExternal,
        LogClient,
        LogPhysics
    }

    public struct PaletteColorDefinition
    {
        public PaletteColorDefinition(PaletteColor key, string displayName, Color color)
        {
            Key = key;
            DisplayName = displayName;
            Color = color;
        }

        public PaletteColor Key { get; }
        public string DisplayName { get; }
        public Color Color { get; }
        public string Hex => "#" + ColorUtility.ToHtmlStringRGBA(Color);
    }

    public static class PaletteColors
    {
        public static readonly Color White = Color.white;
        public static readonly Color TextPrimary = White;
        public static readonly Color TextDisabled = new Color(1f, 1f, 1f, 0.23137255f);

        public static readonly Color UiOverlay = new Color(0f, 0f, 0f, 0.34901962f);
        public static readonly Color UiOverlayHighlighted = new Color(0f, 0f, 0f, 0.7490196f);
        public static readonly Color UiOverlayDisabled = new Color(0f, 0f, 0f, 0.6f);

        public static readonly Color LightGreen = new Color32(42, 193, 50, 255);
        public static readonly Color NormalGreen = new Color32(35, 155, 42, 255);
        public static readonly Color LightBlue = new Color32(80, 115, 216, 255);
        public static readonly Color LightRed = new Color32(155, 31, 31, 255);

        public static readonly Color LogBlue = FromHex("#90C4FE");
        public static readonly Color LogGreen = FromHex("#88B36B");
        public static readonly Color LogRed = FromHex("#8D3131");
        public static readonly Color LogServer = FromHex("#A645A6");
        public static readonly Color LogExternal = FromHex("#E6DC33");
        public static readonly Color LogClient = FromHex("#B94949");
        public static readonly Color LogPhysics = FromHex("#678DB8");

        private static readonly PaletteColorDefinition[] _colors =
        {
            new PaletteColorDefinition(PaletteColor.White, "White", White),
            new PaletteColorDefinition(PaletteColor.TextPrimary, "Text Primary", TextPrimary),
            new PaletteColorDefinition(PaletteColor.TextDisabled, "Text Disabled", TextDisabled),
            new PaletteColorDefinition(PaletteColor.UiOverlay, "UI Overlay", UiOverlay),
            new PaletteColorDefinition(PaletteColor.UiOverlayHighlighted, "UI Overlay Highlighted", UiOverlayHighlighted),
            new PaletteColorDefinition(PaletteColor.UiOverlayDisabled, "UI Overlay Disabled", UiOverlayDisabled),
            new PaletteColorDefinition(PaletteColor.LightGreen, "Light Green", LightGreen),
            new PaletteColorDefinition(PaletteColor.NormalGreen, "Normal Green", NormalGreen),
            new PaletteColorDefinition(PaletteColor.LightBlue, "Light Blue", LightBlue),
            new PaletteColorDefinition(PaletteColor.LightRed, "Light Red", LightRed),
            new PaletteColorDefinition(PaletteColor.LogBlue, "Log Blue", LogBlue),
            new PaletteColorDefinition(PaletteColor.LogGreen, "Log Green", LogGreen),
            new PaletteColorDefinition(PaletteColor.LogRed, "Log Red", LogRed),
            new PaletteColorDefinition(PaletteColor.LogServer, "Log Server", LogServer),
            new PaletteColorDefinition(PaletteColor.LogExternal, "Log External", LogExternal),
            new PaletteColorDefinition(PaletteColor.LogClient, "Log Client", LogClient),
            new PaletteColorDefinition(PaletteColor.LogPhysics, "Log Physics", LogPhysics)
        };

        private static readonly Dictionary<PaletteColor, Color> _colorLookup = BuildLookup();

        public static IReadOnlyList<PaletteColorDefinition> All => _colors;

        public static Color Get(PaletteColor color)
        {
            return _colorLookup.TryGetValue(color, out Color value) ? value : White;
        }

        public static bool TryGet(PaletteColor color, out Color value)
        {
            return _colorLookup.TryGetValue(color, out value);
        }

        public static string GetHex(PaletteColor color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(Get(color));
        }

        private static Dictionary<PaletteColor, Color> BuildLookup()
        {
            var lookup = new Dictionary<PaletteColor, Color>();

            foreach (PaletteColorDefinition color in _colors)
            {
                lookup[color.Key] = color.Color;
            }

            return lookup;
        }

        private static Color FromHex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : White;
        }
    }
}
