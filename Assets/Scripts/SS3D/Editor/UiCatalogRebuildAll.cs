#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Umbrella rebuild for UI path catalogs (interim until TECH_DEBT 1.4/1.5 Addressables pipeline).
    /// Do not add another per-surface Rebuild Asset Catalog MenuItem — call into this or
    /// <see cref="UiCatalogBuilderKit"/>.
    /// </summary>
    public static class UiCatalogRebuildAll
    {
        [MenuItem("SS3D/Data/Rebuild All UI Catalogs")]
        public static void RebuildAllMenu()
        {
            if (!TryRebuildAll(out string error))
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog("UI Catalogs", error, "OK");
                return;
            }

            Debug.Log("Rebuilt Machine UI, Main HUD, UI Shell, Storage Panel, Lobby, Bug Report, and Main Menu asset catalogs.");
            EditorUtility.DisplayDialog(
                "UI Catalogs",
                "Rebuilt Machine UI, Main HUD, UI Shell, Storage Panel, Lobby, Bug Report, and Main Menu asset catalogs.",
                "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Editor.UiCatalogRebuildAll.RebuildAllBatch</c></summary>
        public static void RebuildAllBatch()
        {
            if (!TryRebuildAll(out string error))
            {
                Debug.LogError(error);
                if (UnityEngine.Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            Debug.Log("[UiCatalogRebuildAll] All UI catalogs rebuilt.");
            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static bool TryRebuildAll(out string error)
        {
            if (!MachineUiAssetCatalogBuilder.TryRebuildCatalog(out error))
            {
                return false;
            }

            if (!MainHudAssetCatalogBuilder.TryRebuildCatalog(out error))
            {
                return false;
            }

            if (!UiShellAssetCatalogBuilder.TryRebuildCatalog(out error))
            {
                return false;
            }

            if (!StoragePanelAssetCatalogBuilder.TryRebuildCatalog(out error))
            {
                return false;
            }

            if (!LobbyAssetCatalogBuilder.TryRebuildCatalog(out error))
            {
                return false;
            }

            if (!BugReportAssetCatalogBuilder.TryRebuildCatalog(out error))
            {
                return false;
            }

            if (!MainMenuAssetCatalogBuilder.TryRebuildCatalog(out error))
            {
                return false;
            }

            error = null;
            return true;
        }
    }
}
#endif
