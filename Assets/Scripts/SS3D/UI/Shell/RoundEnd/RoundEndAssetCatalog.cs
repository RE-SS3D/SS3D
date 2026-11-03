using SS3D.UI.Shell.Catalog;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell.RoundEnd
{
    /// <summary>The round-end summary stylesheet, shared by the in-round overlay and the lobby's Last Round tab.</summary>
    public sealed class RoundEndAssetCatalog : UiAssetCatalogBase
    {
        [SerializeField] private StyleSheet _style;
        public StyleSheet Style => _style;

        protected override bool HasSurfaceAssets(out string missingField)
        {
            missingField = _style == null ? nameof(_style) : null;
            return missingField == null;
        }

#if UNITY_EDITOR
        public void EditorAssign(PanelSettings settings, StyleSheet tokens, StyleSheet typography, StyleSheet style)
        {
            EditorAssignShared(settings, tokens, typography);
            _style = style;
        }
#endif
    }
}
