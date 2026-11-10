#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SS3D.UI.Examine;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds the committed <see cref="ExamineOverlayAssetCatalog"/> from <see cref="ExamineOverlayAssetPaths"/>.
    /// </summary>
    public static class ExamineOverlayAssetCatalogBuilder
    {
        /// <summary>Called by <see cref="UiCatalogRebuildAll.TryRebuildAll"/> (SS3D → Data → Rebuild All UI Catalogs).</summary>
        public static bool TryRebuildCatalog(out string error)
        {
            error = null;
            List<string> missing = new();

            StyleSheet examineStyle = LoadRequired<StyleSheet>(ExamineOverlayAssetPaths.StyleSheet, missing);
            StyleSheet inventorySlotStyle = LoadRequired<StyleSheet>(ExamineOverlayAssetPaths.InventorySlotStyle, missing);
            StyleSheet diegeticTokensStyle = LoadRequired<StyleSheet>(ExamineOverlayAssetPaths.DiegeticTokensStyle, missing);
            StyleSheet machineWindowStyle = LoadRequired<StyleSheet>(ExamineOverlayAssetPaths.MachineWindowStyle, missing);

            CharacterExamineIconSet icons = new()
            {
                Head = LoadRequiredSprite("BeepHead", missing),
                Eyes = LoadRequiredSprite("Eyes", missing),
                Face = LoadRequiredSprite("Face", missing),
                Ears = LoadRequiredSprite("Ears", missing),
                Suit = LoadRequiredSprite("Suit", missing),
                Shirt = LoadRequiredSprite("Shirt", missing),
                // Same pair-of-hands silhouette as the Main HUD equipment doll's Gloves slot.
                Gloves = LoadRequiredSprite("Hands", missing),
                Back = LoadRequiredSprite("BeepBack", missing),
                Feet = LoadRequiredSprite("Feet", missing),
                Belt = LoadRequiredSprite("Waist", missing),
                IdCard = LoadRequiredSprite("Neck", missing),
                Pocket = LoadRequiredSprite("Pocket", missing),
                HandLeft = LoadRequiredSprite("HandLeft", missing),
                HandRight = LoadRequiredSprite("HandRight", missing),
            };

            if (missing.Count > 0)
            {
                error = "Examine overlay asset catalog rebuild failed. Missing assets:\n- "
                    + string.Join("\n- ", missing);
                return false;
            }

            string directory = Path.GetDirectoryName(ExamineOverlayAssetPaths.CatalogAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            ExamineOverlayAssetCatalog catalog =
                AssetDatabase.LoadAssetAtPath<ExamineOverlayAssetCatalog>(ExamineOverlayAssetPaths.CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ExamineOverlayAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, ExamineOverlayAssetPaths.CatalogAssetPath);
            }

            catalog.EditorAssign(examineStyle, inventorySlotStyle, diegeticTokensStyle, machineWindowStyle, icons);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
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

        private static Sprite LoadRequiredSprite(string fileName, List<string> missing)
        {
            string path = $"{ExamineOverlayAssetPaths.IconRoot}{fileName}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                missing.Add(path);
            }

            return sprite;
        }
    }
}
#endif
