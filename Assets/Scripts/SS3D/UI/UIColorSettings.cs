using UnityEngine;

namespace SS3D.UI
{
    /// <summary>
    /// Centralized ScriptableObject to manage UI colors across the project.
    /// Allows designers to change one color here and reflect it everywhere.
    /// </summary>
    [CreateAssetMenu(fileName = "UIColorSettings", menuName = "SS3D/UI/Color Settings")]
    public class UIColorSettings : ScriptableObject
    {
        [Header("Primary Colors")]
        public Color Primary = new Color(0.1f, 0.1f, 0.1f, 1f);
        public Color Secondary = new Color(0.2f, 0.2f, 0.2f, 1f);
        public Color Accent = new Color(0f, 0.5f, 1f, 1f);

        [Header("Button States")]
        public Color ButtonNormal = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color ButtonHover = new Color(0.4f, 0.4f, 0.4f, 1f);
        public Color ButtonPressed = new Color(0.2f, 0.2f, 0.2f, 1f);
        public Color ButtonDisabled = new Color(0.15f, 0.15f, 0.15f, 0.5f);

        [Header("Text Colors")]
        public Color TextPrimary = Color.white;
        public Color TextSecondary = Color.gray;
        public Color TextHighlight = Color.yellow;

        [Header("Feedback Colors")]
        public Color Success = Color.green;
        public Color Warning = Color.yellow;
        public Color Danger = Color.red;
        public Color Info = Color.cyan;

        /// <summary>
        /// Helper enum to select colors from the inspector in other scripts.
        /// </summary>
        public enum UIColorType
        {
            Primary, Secondary, Accent,
            ButtonNormal, ButtonHover, ButtonPressed, ButtonDisabled,
            TextPrimary, TextSecondary, TextHighlight,
            Success, Warning, Danger, Info
        }

        public Color GetColor(UIColorType type)
        {
            return type switch
            {
                UIColorType.Primary => Primary,
                UIColorType.Secondary => Secondary,
                UIColorType.Accent => Accent,
                UIColorType.ButtonNormal => ButtonNormal,
                UIColorType.ButtonHover => ButtonHover,
                UIColorType.ButtonPressed => ButtonPressed,
                UIColorType.ButtonDisabled => ButtonDisabled,
                UIColorType.TextPrimary => TextPrimary,
                UIColorType.TextSecondary => TextSecondary,
                UIColorType.TextHighlight => TextHighlight,
                UIColorType.Success => Success,
                UIColorType.Warning => Warning,
                UIColorType.Danger => Danger,
                UIColorType.Info => Info,
                _ => Color.white
            };
        }
    }
}
