using System.Text;
using UnityEngine;

namespace SS3D.Systems.Entities.Character
{
    /// <summary>
    /// A player's saved character as sent from the lobby and replicated on their spawned body.
    /// Body values are 0–1 (jaw is bipolar, 0.5 = neutral); style indices refer to the shared
    /// <see cref="IHumanoidStyleSource"/> (0 = none).
    /// </summary>
    public struct CharacterAppearance
    {
        public const int MaxNameLength = 32;

        public string Name;
        public float Female;
        public float Breasts;
        public float Fat;
        public float Muscle;
        public float Jaw;
        public float Height;
        public int HairStyle;
        public int BeardStyle;
        // Color32, not Color: FishNet's default Color serializer packs each channel as
        // (byte)(value * 100), which truncates palette colors on the wire.
        public Color32 HairColor;
        public Color32 BeardColor;
        public Color32 SkinColor;
        public Color32 EyeColor;
        public Color32 EyebrowColor;
        public string EyebrowStyle;

        /// <summary>
        /// False for the default (unset) value: <see cref="Sanitized"/> always yields opaque colors,
        /// so one replicated field carries both the value and whether a body has one.
        /// </summary>
        public bool IsSet => HairColor.a > 0;

        /// <summary>
        /// Server-side cleanup of client input: printable single-line name without rich-text
        /// brackets, clamped body values and opaque colors.
        /// </summary>
        public CharacterAppearance Sanitized()
        {
            CharacterAppearance result = this;
            result.Name = SanitizeName(Name);
            result.Female = Clamp(Female);
            result.Breasts = Clamp(Breasts);
            result.Fat = Clamp(Fat);
            result.Muscle = Clamp(Muscle);
            result.Jaw = Clamp(Jaw);
            result.Height = Clamp(Height);
            result.HairStyle = Mathf.Max(0, HairStyle);
            result.BeardStyle = Mathf.Max(0, BeardStyle);
            result.HairColor = Opaque(HairColor);
            result.BeardColor = Opaque(BeardColor);
            result.SkinColor = Opaque(SkinColor);
            result.EyeColor = Opaque(EyeColor);
            result.EyebrowColor = Opaque(EyebrowColor);
            result.EyebrowStyle = NormalizeEyebrowStyle(EyebrowStyle);
            return result;
        }

        public static string NormalizeEyebrowStyle(string id) => id?.ToLowerInvariant() switch
        {
            "none" => "none",
            "humaneyebrowsbold" or "thick" or "bushy" => "humaneyebrowsbold",
            "humaneyebrowscircle" => "humaneyebrowscircle",
            "humaneyebrowsclassic" or "angled" => "humaneyebrowsclassic",
            "humaneyebrowsdainty" or "thin" => "humaneyebrowsdainty",
            _ => "humaneyebrowsdefault",
        };

        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            StringBuilder builder = new(name.Length);
            foreach (char c in name)
            {
                if (!char.IsControl(c) && c != '<' && c != '>')
                {
                    builder.Append(c);
                }
            }

            string trimmed = builder.ToString().Trim();
            return trimmed.Length > MaxNameLength ? trimmed.Substring(0, MaxNameLength).TrimEnd() : trimmed;
        }

        // NaN fails both comparisons in Clamp01 and would survive; treat it as neutral.
        private static float Clamp(float value) => float.IsNaN(value) ? 0.5f : Mathf.Clamp01(value);

        private static Color32 Opaque(Color32 color) => new(color.r, color.g, color.b, 255);
    }
}
