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
    }
}
