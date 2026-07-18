#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SS3D.UI.MainHud;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds the committed <see cref="MainHudAssetCatalog"/> from <see cref="MainHudAssetPaths"/>.
    /// </summary>
    public static class MainHudAssetCatalogBuilder
    {
        [MenuItem("SS3D/Main HUD/Rebuild Asset Catalog")]
        public static void RebuildCatalogMenu()
        {
            if (!TryRebuildCatalog(out string error))
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog("Main HUD Asset Catalog", error, "OK");
                return;
            }

            Debug.Log($"Rebuilt Main HUD asset catalog at {MainHudAssetPaths.CatalogAssetPath}");
        }

        public static bool TryRebuildCatalog(out string error)
        {
            error = null;
            List<string> missing = new();

            PanelSettings panelSettings = LoadRequired<PanelSettings>(MainHudAssetPaths.PanelSettings, missing);
            StyleSheet mainHudStyle = LoadRequired<StyleSheet>(MainHudAssetPaths.MainHudStyle, missing);
            StyleSheet alertStyle = LoadRequired<StyleSheet>(MainHudAssetPaths.AlertIconStackStyle, missing);
            StyleSheet intentStyle = LoadRequired<StyleSheet>(MainHudAssetPaths.IntentModuleStyle, missing);
            StyleSheet handsStyle = LoadRequired<StyleSheet>(MainHudAssetPaths.HandsGearStripStyle, missing);
            StyleSheet equipmentStyle = LoadRequired<StyleSheet>(MainHudAssetPaths.EquipmentGridStyle, missing);
            StyleSheet inventorySlotStyle = LoadRequired<StyleSheet>(MainHudAssetPaths.InventorySlotStyle, missing);

            MainHudIconSet icons = new()
            {
                Head = LoadRequiredSprite("BeepHead", missing),
                Eyes = LoadRequiredSprite("Eyes", missing),
                Face = LoadRequiredSprite("Face", missing),
                Ears = LoadRequiredSprite("Ears", missing),
                HandLeft = LoadRequiredSprite("HandLeft", missing),
                HandRight = LoadRequiredSprite("HandRight", missing),
                Shirt = LoadRequiredSprite("Shirt", missing),
                Feet = LoadRequiredSprite("Feet", missing),
                Belt = LoadRequiredSprite("Waist", missing),
                Id = LoadRequiredSprite("Neck", missing),
                Pocket = LoadRequiredSprite("Pocket", missing),
                Back = LoadRequiredSprite("BeepBack", missing),
            };

            if (missing.Count > 0)
            {
                error = "Main HUD asset catalog rebuild failed. Missing assets:\n- "
                    + string.Join("\n- ", missing);
                return false;
            }

            string directory = Path.GetDirectoryName(MainHudAssetPaths.CatalogAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            MainHudAssetCatalog catalog =
                AssetDatabase.LoadAssetAtPath<MainHudAssetCatalog>(MainHudAssetPaths.CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MainHudAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, MainHudAssetPaths.CatalogAssetPath);
            }

            catalog.EditorAssign(
                panelSettings,
                mainHudStyle,
                alertStyle,
                intentStyle,
                handsStyle,
                equipmentStyle,
                inventorySlotStyle,
                icons);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static T LoadRequired<T>(string path, List<string> missing) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                missing.Add($"{typeof(T).Name}: {path}");
            }

            return asset;
        }

        private static Sprite LoadRequiredSprite(string fileName, List<string> missing)
        {
            string path = $"{MainHudAssetPaths.IconRoot}{fileName}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                missing.Add($"Sprite: {path}");
            }

            return sprite;
        }
    }
}
#endif
