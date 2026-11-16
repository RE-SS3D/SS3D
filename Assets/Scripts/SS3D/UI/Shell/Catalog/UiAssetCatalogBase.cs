using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell.Catalog
{
    /// <summary>
    /// Base for the per-surface committed asset catalogs (machine interface, main HUD, storage
    /// panel, UI shell, ...), each loaded at runtime via <c>Resources.Load</c> and rebuilt from a
    /// surface-specific <c>*AssetPaths</c> class through an Editor menu. Holds the fields every
    /// surface duplicated (panel settings, shared tokens); surface-specific fields live on the
    /// derived type.
    /// </summary>
    public abstract class UiAssetCatalogBase : ScriptableObject
    {
        [SerializeField]
        private PanelSettings _panelSettings;

        [SerializeField]
        private StyleSheet _ss3dTokensStyle;

        [SerializeField]
        private StyleSheet _ss3dTypographyStyle;

        public PanelSettings PanelSettings => _panelSettings;

        public StyleSheet Ss3dTokensStyle => _ss3dTokensStyle;

        public StyleSheet Ss3dTypographyStyle => _ss3dTypographyStyle;

        public bool HasRequiredAssets(out string missingField)
        {
            if (_panelSettings == null)
            {
                missingField = nameof(_panelSettings);
                return false;
            }

            if (_ss3dTokensStyle == null || _ss3dTypographyStyle == null)
            {
                missingField = "shared tokens";
                return false;
            }

            return HasSurfaceAssets(out missingField);
        }

        protected abstract bool HasSurfaceAssets(out string missingField);

#if UNITY_EDITOR
        protected void EditorAssignShared(PanelSettings panelSettings, StyleSheet ss3dTokensStyle, StyleSheet ss3dTypographyStyle)
        {
            _panelSettings = panelSettings;
            _ss3dTokensStyle = ss3dTokensStyle;
            _ss3dTypographyStyle = ss3dTypographyStyle;
        }
#endif
    }
}
