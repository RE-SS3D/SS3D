using System.Collections.Generic;
using System.IO;
using SS3D.UI.Shell.Fonts;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace SS3D.Editor
{
    /// <summary>
    /// Bakes every UI Toolkit font asset under <see cref="FontRoot"/> into a static atlas holding
    /// <see cref="UiFontFallbacks.Characters"/> and records its source font for the runtime fallback.
    /// Dynamic atlases rewrite their asset with every new glyph drawn, which churned git after each
    /// play session. Run it after adding a UI font asset or a character; commit the result.
    /// </summary>
    public static class UiFontBaker
    {
        private const string FontRoot = "Assets/Content/Systems/UI";
        private const string CatalogPath = "Assets/Content/Systems/UI/Font/Resources/" + UiFontFallbacks.ResourceName + ".asset";

        [MenuItem("SS3D/Build/Bake Static UI Fonts")]
        public static void Bake()
        {
            UiFontFallbacks catalog = AssetDatabase.LoadAssetAtPath<UiFontFallbacks>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
                catalog = ScriptableObject.CreateInstance<UiFontFallbacks>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Entries.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:FontAsset", new[] { FontRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FontAsset asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                Font source = SourceFont(asset);
                if (asset == null || source == null)
                {
                    Debug.LogWarning($"[UiFontBaker] {path}: no source font, left as it is");
                    continue;
                }

                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                asset.isMultiAtlasTexturesEnabled = true;
                asset.ClearFontAssetData(false);
                asset.TryAddCharacters(UiFontFallbacks.Characters, out string missing);
                asset.atlasPopulationMode = AtlasPopulationMode.Static;

                SerializedObject serialized = new(asset);
                SerializedProperty clearOnBuild = serialized.FindProperty("m_ClearDynamicDataOnBuild");
                if (clearOnBuild != null)
                    clearOnBuild.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);

                catalog.Entries.Add(new UiFontFallbacks.Entry { Asset = asset, Source = source });
                Debug.Log($"[UiFontBaker] {path}: static, {asset.characterTable.Count} characters"
                    + (string.IsNullOrEmpty(missing) ? "" : $"; not in the font: {missing}"));
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The asset's source font; a static asset may have dropped the reference but keeps its GUID.</summary>
        private static Font SourceFont(FontAsset asset)
        {
            if (asset == null)
                return null;
            if (asset.sourceFontFile != null)
                return asset.sourceFontFile;

            string guid = new SerializedObject(asset).FindProperty("m_SourceFontFileGUID")?.stringValue;
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
