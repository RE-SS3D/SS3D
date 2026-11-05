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
    /// Menu: use <see cref="UiCatalogRebuildAll"/>.
    /// </summary>
    public static class MainHudAssetCatalogBuilder
    {
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
                Gloves = LoadRequiredSprite("Hands", missing),
                Shirt = LoadRequiredSprite("Shirt", missing),
                Feet = LoadRequiredSprite("Feet", missing),
                Belt = LoadRequiredSprite("Waist", missing),
                Id = LoadRequiredSprite("Neck", missing),
                Pocket = LoadRequiredSprite("Pocket", missing),
                Back = LoadRequiredSprite("BeepBack", missing),
            };

            AlertIconSet alertIcons = new()
            {
                Fire = LoadRequiredAlertSprite("hot-fire", missing),
                Hot = LoadRequiredAlertSprite("hot-thermometer", missing),
                Cold = LoadRequiredAlertSprite("cold-thermometer", missing),
                LowPressure = LoadRequiredAlertSprite("pressure-low", missing),
                HighPressure = LoadRequiredAlertSprite("pressure-high", missing),
                Radiation = LoadRequiredAlertSprite("radiation-trefoil", missing),
                Hunger = LoadRequiredAlertSprite("hunger", missing),
                Thirst = LoadRequiredAlertSprite("thirst-droplet", missing),
                Pulling = LoadRequiredAlertSprite("pulling", missing),
                Restrained = LoadRequiredAlertSprite("restrained-cuffs", missing),
                LowOxygen = LoadRequiredAlertSprite("low-oxygen", missing),
                Dying = LoadRequiredAlertSprite("dying-heartbeat", missing),
                Bleeding = LoadRequiredAlertSprite("bleeding-droplet", missing),
                CardiacArrest = LoadRequiredAlertSprite("cardiac-arrest", missing),
                SealBreach = LoadRequiredAlertSprite("seal-breach", missing),
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
                icons,
                alertIcons);
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

        private static Sprite LoadRequiredAlertSprite(string fileName, List<string> missing)
        {
            string path = $"{MainHudAssetPaths.AlertIconRoot}{fileName}.png";
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
