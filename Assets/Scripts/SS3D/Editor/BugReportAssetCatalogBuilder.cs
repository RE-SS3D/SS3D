#if UNITY_EDITOR
using System.Collections.Generic;
using SS3D.UI.Shell.BugReport;
using SS3D.UI.Shell.Catalog;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    public static class BugReportAssetCatalogBuilder
    {
        public static bool TryRebuildCatalog(out string error)
        {
            List<string> missing = new();
            PanelSettings settings = UiCatalogBuilderKit.LoadRequired<PanelSettings>(UiShellAssetPaths.PanelSettings, missing);
            StyleSheet tokens = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTokens, missing);
            StyleSheet typography = UiCatalogBuilderKit.LoadRequired<StyleSheet>(UiShellAssetPaths.Ss3dTypography, missing);
            VisualTreeAsset template = UiCatalogBuilderKit.LoadRequired<VisualTreeAsset>(BugReportAssetPaths.Template, missing);
            StyleSheet style = UiCatalogBuilderKit.LoadRequired<StyleSheet>(BugReportAssetPaths.Style, missing);
            BugReportAssetCatalog catalog = UiCatalogBuilderKit.LoadOrCreateCatalogAsset<BugReportAssetCatalog>(BugReportAssetPaths.Catalog);
            catalog.EditorAssign(settings, tokens, typography, template, style);
            return UiCatalogBuilderKit.FinalizeSave(missing, "Bug report visuals", catalog, out error);
        }
    }
}
#endif
