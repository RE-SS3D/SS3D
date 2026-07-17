#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SS3D.UI.MachineInterface;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds the committed <see cref="MachineUiAssetCatalog"/> from <see cref="MachineUiAssetPaths"/>
    /// and strips obsolete SerializeField template refs from the Game-scene host.
    /// </summary>
    public static class MachineUiAssetCatalogBuilder
    {
        private const string GameScenePath = "Assets/Content/Scenes/Game.unity";

        [MenuItem("SS3D/Machine Interface/Rebuild Asset Catalog")]
        public static void RebuildCatalogMenu()
        {
            if (!TryRebuildCatalog(out string error))
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog("Machine UI Asset Catalog", error, "OK");
                return;
            }

            Debug.Log($"Rebuilt machine UI asset catalog at {MachineUiAssetPaths.CatalogAssetPath}");
        }

        [MenuItem("SS3D/Machine Interface/Clear Host Scene Template Refs")]
        public static void ClearHostSceneRefsMenu()
        {
            if (!TryClearHostSceneRefs(out string error))
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog("Machine UI Host Cleanup", error, "OK");
                return;
            }

            Debug.Log("Cleared obsolete MachineInterfaceHost template fields from Game.unity.");
        }

        /// <summary>
        /// Batchmode entry: rebuild catalog and clear Game.unity host template fields.
        /// </summary>
        public static void RebuildCatalogAndClearHost()
        {
            if (!TryRebuildCatalog(out string rebuildError))
            {
                Debug.LogError(rebuildError);
                EditorApplication.Exit(1);
                return;
            }

            if (!TryClearHostSceneRefs(out string clearError))
            {
                Debug.LogError(clearError);
                EditorApplication.Exit(1);
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Machine UI path catalog rebuild + Game.unity host cleanup succeeded.");
            EditorApplication.Exit(0);
        }

        public static bool TryRebuildCatalog(out string error)
        {
            error = null;
            List<string> missing = new();

            PanelSettings panelSettings = LoadRequired<PanelSettings>(MachineUiAssetPaths.PanelSettings, missing);
            StyleSheet machineWindowStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.MachineWindowStyle, missing);
            StyleSheet ss3dTokens = LoadRequired<StyleSheet>(MachineUiAssetPaths.Ss3dTokens, missing);
            StyleSheet ss3dTypography = LoadRequired<StyleSheet>(MachineUiAssetPaths.Ss3dTypography, missing);
            StyleSheet diegeticTokens = LoadRequired<StyleSheet>(MachineUiAssetPaths.DiegeticTokens, missing);
            StyleSheet diegeticTones = LoadRequired<StyleSheet>(MachineUiAssetPaths.DiegeticTones, missing);

            VisualTreeAsset apcTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.ApcTemplate, missing);
            StyleSheet apcStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.ApcTemplateStyle, missing);
            StyleSheet[] apcComponents = LoadStyleSheets(MachineUiAssetPaths.ApcComponentStyles, missing);

            VisualTreeAsset smesTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.SmesTemplate, missing);
            StyleSheet smesStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.SmesTemplateStyle, missing);
            StyleSheet[] smesComponents = LoadStyleSheets(MachineUiAssetPaths.SmesComponentStyles, missing);

            VisualTreeAsset vendingTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.VendingTemplate, missing);
            StyleSheet vendingStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.VendingTemplateStyle, missing);
            StyleSheet[] vendingComponents = LoadStyleSheets(MachineUiAssetPaths.VendingComponentStyles, missing);

            VisualTreeAsset idConsoleTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.IdConsoleTemplate, missing);
            StyleSheet idConsoleStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.IdConsoleTemplateStyle, missing);

            VisualTreeAsset gasPumpTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.GasPumpTemplate, missing);
            StyleSheet gasPumpStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.GasPumpTemplateStyle, missing);
            StyleSheet[] gasPumpComponents = LoadStyleSheets(
                MachineUiAssetPaths.BuildAtmosComponentStyles(includeAirAlarm: false, includeVent: true),
                missing);

            VisualTreeAsset airAlarmTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.AirAlarmTemplate, missing);
            StyleSheet airAlarmStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.AirAlarmTemplateStyle, missing);
            StyleSheet[] airAlarmComponents = LoadStyleSheets(
                MachineUiAssetPaths.BuildAtmosComponentStyles(includeAirAlarm: true, includeVent: false),
                missing);

            VisualTreeAsset scrubberTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.ScrubberTemplate, missing);
            StyleSheet scrubberStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.ScrubberTemplateStyle, missing);
            StyleSheet[] scrubberComponents = LoadStyleSheets(
                MachineUiAssetPaths.BuildAtmosComponentStyles(includeAirAlarm: false, includeVent: false),
                missing);

            VisualTreeAsset ventTemplate = LoadRequired<VisualTreeAsset>(MachineUiAssetPaths.VentTemplate, missing);
            StyleSheet ventStyle = LoadRequired<StyleSheet>(MachineUiAssetPaths.VentTemplateStyle, missing);
            StyleSheet[] ventComponents = LoadStyleSheets(
                MachineUiAssetPaths.BuildAtmosComponentStyles(includeAirAlarm: false, includeVent: true),
                missing);

            if (missing.Count > 0)
            {
                error = "Machine UI asset catalog rebuild failed. Missing assets:\n- "
                    + string.Join("\n- ", missing);
                return false;
            }

            string directory = Path.GetDirectoryName(MachineUiAssetPaths.CatalogAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            MachineUiAssetCatalog catalog =
                AssetDatabase.LoadAssetAtPath<MachineUiAssetCatalog>(MachineUiAssetPaths.CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MachineUiAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, MachineUiAssetPaths.CatalogAssetPath);
            }

            catalog.EditorAssign(
                panelSettings,
                machineWindowStyle,
                ss3dTokens,
                ss3dTypography,
                diegeticTokens,
                diegeticTones,
                apcTemplate,
                apcStyle,
                apcComponents,
                smesTemplate,
                smesStyle,
                smesComponents,
                vendingTemplate,
                vendingStyle,
                vendingComponents,
                idConsoleTemplate,
                idConsoleStyle,
                gasPumpTemplate,
                gasPumpStyle,
                gasPumpComponents,
                airAlarmTemplate,
                airAlarmStyle,
                airAlarmComponents,
                scrubberTemplate,
                scrubberStyle,
                scrubberComponents,
                ventTemplate,
                ventStyle,
                ventComponents);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        public static bool TryClearHostSceneRefs(out string error)
        {
            error = null;
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            MachineInterfaceHost[] hosts = Object.FindObjectsByType<MachineInterfaceHost>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (hosts.Length == 0)
            {
                error = $"No {nameof(MachineInterfaceHost)} found in {GameScenePath}.";
                return false;
            }

            for (int i = 0; i < hosts.Length; i++)
            {
                // After host SerializeFields were removed, saving the scene drops orphaned YAML.
                EditorUtility.SetDirty(hosts[i]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        private static T LoadRequired<T>(string path, List<string> missing) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                missing.Add(path);
            }

            return asset;
        }

        private static StyleSheet[] LoadStyleSheets(string[] paths, List<string> missing)
        {
            StyleSheet[] sheets = new StyleSheet[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                sheets[i] = LoadRequired<StyleSheet>(paths[i], missing);
            }

            return sheets;
        }
    }
}
#endif
