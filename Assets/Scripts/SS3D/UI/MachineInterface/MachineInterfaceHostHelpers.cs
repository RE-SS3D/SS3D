using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    internal static class MachineInterfaceHostHelpers
    {
        public static void ApplyTemplateStyle(VisualElement template, StyleSheet styleSheet)
        {
            if (styleSheet != null)
            {
                template.styleSheets.Add(styleSheet);
            }
        }

        public static void ApplyStyleSheets(VisualElement template, IEnumerable<StyleSheet> styleSheets)
        {
            if (styleSheets == null)
            {
                return;
            }

            foreach (StyleSheet styleSheet in styleSheets)
            {
                ApplyTemplateStyle(template, styleSheet);
            }
        }

        public static void ApplyStyleSheets(VisualElement template, params StyleSheet[] styleSheets)
        {
            ApplyStyleSheets(template, (IEnumerable<StyleSheet>)styleSheets);
        }
    }
}
