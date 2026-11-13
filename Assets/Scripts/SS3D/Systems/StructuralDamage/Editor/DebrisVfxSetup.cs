#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>
    /// Packs the SS3D-Art debris sprites listed in <see cref="DebrisSpriteVfx.Sprites"/> into one grid atlas, one
    /// row per <see cref="DebrisKind"/>, and points <see cref="DebrisVfxCatalog"/> at it.
    /// </summary>
    public static class DebrisVfxSetup
    {
        public const string AtlasPath = DebrisSpriteVfx.SourceFolder + "/Atlas/DebrisAtlas.png";
        private const string CatalogPath = "Assets/Resources/DebrisVfxCatalog.asset";

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.StructuralDamage.Editor.DebrisVfxSetup.SetupBatch</c>
        /// Prefer <see cref="StructuralDamageContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            SetupAll();
            Debug.Log("[DebrisVfxSetup] Done.");
            if (Application.isBatchMode)
                EditorApplication.Exit(DebrisVfxCatalog.Instance != null && DebrisVfxCatalog.Instance.Atlas != null ? 0 : 1);
        }

        public static void SetupAll()
        {
            Texture2D atlas = BuildAtlas();
            if (atlas == null)
                return;

            DebrisVfxCatalog catalog = AssetDatabase.LoadAssetAtPath<DebrisVfxCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DebrisVfxCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject so = new SerializedObject(catalog);
            so.FindProperty("_atlas").objectReferenceValue = atlas;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D BuildAtlas()
        {
            const int cell = DebrisSpriteVfx.CellSize;
            const int width = DebrisSpriteVfx.Columns * cell;
            const int height = DebrisSpriteVfx.Rows * cell;
            Texture2D packed = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            packed.SetPixels32(new Color32[width * height]);

            for (int row = 0; row < DebrisSpriteVfx.Sprites.Length; row++)
            {
                string[] names = DebrisSpriteVfx.Sprites[row];
                for (int column = 0; column < names.Length; column++)
                {
                    string source = $"{DebrisSpriteVfx.SourceFolder}/{names[column]}.png";
                    if (!File.Exists(source))
                    {
                        Debug.LogError($"[DebrisVfxSetup] Missing debris sprite {source}");
                        Object.DestroyImmediate(packed);
                        return null;
                    }

                    Texture2D sprite = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    sprite.LoadImage(File.ReadAllBytes(source));
                    if (sprite.width != cell || sprite.height != cell)
                    {
                        Debug.LogError($"[DebrisVfxSetup] {source} is {sprite.width}x{sprite.height}, expected {cell}x{cell}");
                        Object.DestroyImmediate(sprite);
                        Object.DestroyImmediate(packed);
                        return null;
                    }

                    // Texture rows count up from the bottom; the particle sheet counts row 0 from the top.
                    int y = (DebrisSpriteVfx.Rows - 1 - row) * cell;
                    packed.SetPixels32(column * cell, y, cell, cell, sprite.GetPixels32());
                    Object.DestroyImmediate(sprite);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AtlasPath));
            File.WriteAllBytes(AtlasPath, packed.EncodeToPNG());
            Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(AtlasPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        }
    }
}
#endif
