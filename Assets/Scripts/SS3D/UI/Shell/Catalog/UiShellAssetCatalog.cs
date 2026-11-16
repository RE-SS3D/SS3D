using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell.Catalog
{
    /// <summary>
    /// Committed resolved refs for <see cref="UiShellSubSystem"/>. Rebuild via
    /// <c>SS3D → Data → Rebuild All UI Catalogs</c> from <see cref="UiShellAssetPaths"/>.
    /// Loaded at runtime with <c>Resources.Load</c>.
    /// </summary>
    [CreateAssetMenu(
        fileName = UiShellAssetPaths.ResourcesCatalogName,
        menuName = "SS3D/UI/UI Shell Asset Catalog")]
    public sealed class UiShellAssetCatalog : UiAssetCatalogBase
    {
        protected override bool HasSurfaceAssets(out string missingField)
        {
            missingField = null;
            return true;
        }

#if UNITY_EDITOR
        public void EditorAssign(PanelSettings panelSettings, StyleSheet ss3dTokensStyle, StyleSheet ss3dTypographyStyle)
        {
            EditorAssignShared(panelSettings, ss3dTokensStyle, ss3dTypographyStyle);
        }
#endif
    }
}
