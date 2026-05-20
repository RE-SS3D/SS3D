using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Semantic keys for shared UI colors used by SS3D views and style assets.
    /// </summary>
    public enum PaletteColor
    {
        TextPrimary,
        TextInverted,
        TextDisabled,
        ButtonBackground,
        ButtonBackgroundHighlighted,
        ButtonBackgroundPressed,
        ButtonBackgroundDisabled,
        LightGreen,
        NormalGreen,
        LightBlue,
        LightRed
    }

    /// <summary>
    /// Metadata for one shared UI color.
    /// </summary>
    public sealed class PaletteColorDefinition
    {
        public PaletteColor Key { get; private set; }
        public string DisplayName { get; private set; }
        public Color Color { get; private set; }
        public string Usage { get; private set; }

        public PaletteColorDefinition(PaletteColor key, string displayName, Color color, string usage)
        {
            Key = key;
            DisplayName = displayName;
            Color = color;
            Usage = usage;
        }
    }

    /// <summary>
    /// Shared UI colors used by SS3D views and style assets.
    /// Keep subsystem-specific colors close to their subsystem instead.
    /// </summary>
    public static class PaletteColors
    {
        public static readonly Color White = Color.white;
        public static readonly Color TextPrimary = White;
        public static readonly Color TextInverted = Color.black;
        public static readonly Color TextDisabled = new Color(1f, 1f, 1f, 0.23137255f);

        public static readonly Color ButtonBackground = new Color(0f, 0f, 0f, 0.34901962f);
        public static readonly Color ButtonBackgroundHighlighted = new Color(0f, 0f, 0f, 0.7490196f);
        public static readonly Color ButtonBackgroundPressed = White;
        public static readonly Color ButtonBackgroundDisabled = new Color(0f, 0f, 0f, 0.6f);

        public static readonly Color LightGreen = new Color32(42, 193, 50, 255);
        public static readonly Color NormalGreen = new Color32(35, 155, 42, 255);
        public static readonly Color LightBlue = new Color32(80, 115, 216, 255);
        public static readonly Color LightRed = new Color32(155, 31, 31, 255);

        private static readonly PaletteColorDefinition[] Definitions =
        {
            new PaletteColorDefinition(PaletteColor.TextPrimary, "Text primary", TextPrimary, "Default text on dark UI backgrounds."),
            new PaletteColorDefinition(PaletteColor.TextInverted, "Text inverted", TextInverted, "Text displayed on light or pressed UI states."),
            new PaletteColorDefinition(PaletteColor.TextDisabled, "Text disabled", TextDisabled, "Text for disabled or unavailable controls."),
            new PaletteColorDefinition(PaletteColor.ButtonBackground, "Button background", ButtonBackground, "Default generic button overlay."),
            new PaletteColorDefinition(PaletteColor.ButtonBackgroundHighlighted, "Button background highlighted", ButtonBackgroundHighlighted, "Hovered or highlighted generic button overlay."),
            new PaletteColorDefinition(PaletteColor.ButtonBackgroundPressed, "Button background pressed", ButtonBackgroundPressed, "Pressed generic button background."),
            new PaletteColorDefinition(PaletteColor.ButtonBackgroundDisabled, "Button background disabled", ButtonBackgroundDisabled, "Disabled generic button overlay."),
            new PaletteColorDefinition(PaletteColor.LightGreen, "Light green", LightGreen, "Positive or success accent color."),
            new PaletteColorDefinition(PaletteColor.NormalGreen, "Normal green", NormalGreen, "Standard green accent color."),
            new PaletteColorDefinition(PaletteColor.LightBlue, "Light blue", LightBlue, "Informational or selected-state accent color."),
            new PaletteColorDefinition(PaletteColor.LightRed, "Light red", LightRed, "Danger or destructive-state accent color.")
        };

        private static readonly Dictionary<PaletteColor, PaletteColorDefinition> DefinitionsByKey = BuildDefinitionsByKey();

        /// <summary>
        /// All shared UI colors, including their semantic key and intended usage.
        /// </summary>
        public static IReadOnlyList<PaletteColorDefinition> All
        {
            get { return Definitions; }
        }

        public static Color GetColor(PaletteColor key)
        {
            return GetDefinition(key).Color;
        }

        public static bool TryGetColor(PaletteColor key, out Color color)
        {
            PaletteColorDefinition definition;
            if (DefinitionsByKey.TryGetValue(key, out definition))
            {
                color = definition.Color;
                return true;
            }

            color = default(Color);
            return false;
        }

        public static PaletteColorDefinition GetDefinition(PaletteColor key)
        {
            PaletteColorDefinition definition;
            if (DefinitionsByKey.TryGetValue(key, out definition))
            {
                return definition;
            }

            throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown palette color key.");
        }

        public static bool TryGetDefinition(PaletteColor key, out PaletteColorDefinition definition)
        {
            return DefinitionsByKey.TryGetValue(key, out definition);
        }

        private static Dictionary<PaletteColor, PaletteColorDefinition> BuildDefinitionsByKey()
        {
            Dictionary<PaletteColor, PaletteColorDefinition> definitionsByKey = new Dictionary<PaletteColor, PaletteColorDefinition>();

            foreach (PaletteColorDefinition definition in Definitions)
            {
                definitionsByKey.Add(definition.Key, definition);
            }

            return definitionsByKey;
        }
    }
}
