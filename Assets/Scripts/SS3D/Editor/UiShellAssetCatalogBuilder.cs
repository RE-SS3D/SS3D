#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SS3D.UI.Shell.Catalog;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds the committed <see cref="UiShellAssetCatalog"/> from <see cref="UiShellAssetPaths"/>.
    /// Menu: use <see cref="UiCatalogRebuildAll"/>.
    /// </summary>
    public static class UiShellAssetCatalogBuilder
    {
        public static bool TryRebuildCatalog(out string error)
        {
            List<string> missing = new();

            PanelSettings panelSettings = UiCatalogBuilderKit.LoadRequired<PanelSettings>(UiShellAssetPaths.PanelSettings, missing);
            StyleSheet ss3dTokens = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTokens, missing);
            StyleSheet ss3dTypography = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTypography, missing);

            UiShellAssetCatalog catalog = UiCatalogBuilderKit.LoadOrCreateCatalogAsset<UiShellAssetCatalog>(UiShellAssetPaths.CatalogAssetPath);
            catalog.EditorAssign(panelSettings, ss3dTokens, ss3dTypography);

            return UiCatalogBuilderKit.FinalizeSave(missing, "UI shell", catalog, out error);
        }
    }
}
#endif
