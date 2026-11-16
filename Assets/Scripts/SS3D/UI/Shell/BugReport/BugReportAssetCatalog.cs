using SS3D.UI.Shell.Catalog;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell.BugReport
{
    public sealed class BugReportAssetCatalog : UiAssetCatalogBase
    {
        [SerializeField] private VisualTreeAsset _template;
        [SerializeField] private StyleSheet _style;
        public VisualTreeAsset Template => _template;
        public StyleSheet Style => _style;

        protected override bool HasSurfaceAssets(out string missingField)
        {
            missingField = null;
            if (_template == null) missingField = nameof(_template);
            else if (_style == null) missingField = nameof(_style);
            return missingField == null;
        }

#if UNITY_EDITOR
        public void EditorAssign(PanelSettings settings, StyleSheet tokens, StyleSheet typography,
            VisualTreeAsset template, StyleSheet style)
        {
            EditorAssignShared(settings, tokens, typography);
            _template = template;
            _style = style;
        }
#endif
    }
}
