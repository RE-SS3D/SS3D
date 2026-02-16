#if UNITY_EDITOR
using JetBrains.Annotations;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public partial class GenericObjectSo
    {
        private const string IconFolderPath = "Assets/Art/Generated/Icons";

        [MenuItem("Tools/SS3D/Create Preview icons for GenericObjectSo")]
        public static void CreateIcons()
        {
            string[] guids = AssetDatabase.FindAssets("t:GenericObjectSo");

            foreach (GenericObjectSo genericObjectSo in guids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GenericObjectSo>)
                .Where(genericObjectSo => genericObjectSo && !genericObjectSo.icon))
            {
                genericObjectSo.CreateIconAsset();
            }

            AssetDatabase.SaveAssets();
        }

        [ContextMenu("Create Icon")]
        public void CreateIconAssetContextMenu()
        {
            CreateIconAsset();
            AssetDatabase.SaveAssetIfDirty(this);
        }

        private void CreateIconAsset()
        {
            Sprite newIcon = CreateIcon();

            if (!newIcon)
            {
                Debug.LogError($"Failed to create icon for {name}");

                return;
            }

            string iconPath = SaveIcon(newIcon);

            if (string.IsNullOrEmpty(iconPath))
            {
                Debug.LogError($"Failed to save icon for {name}");

                return;
            }

            icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            EditorUtility.SetDirty(this);
        }

        [CanBeNull]
        private Sprite CreateIcon()
        {
            if (!PrefabAsset)
            {
                Debug.LogError($"{name} has no ObjectAssetReference assigned.");

                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(PrefabAsset.Id);

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"{name} has an invalid ObjectAssetReference assigned.");

                return null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Transform prefabTransform = prefab.transform;
            Shader shader = Shader.Find("Unlit/ObjectIcon");

            RuntimePreviewGenerator.MarkTextureNonReadable = false;
            Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(prefabTransform, shader, null, 128, 128, true);

            if (texture)
            {
                return Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f));
            }

            Debug.LogError("Failed to generate icon for " + name);

            return null;
        }

        [CanBeNull]
        private string SaveIcon([NotNull] Sprite iconToSave)
        {
            if (!Directory.Exists(IconFolderPath))
            {
                Directory.CreateDirectory(IconFolderPath);
            }

            string path = Path.Combine($"{IconFolderPath}", $"{PrefabAsset.name}_Icon.png");
            byte[] pngData = iconToSave.texture.EncodeToPNG();

            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
                AssetDatabase.ImportAsset(path);

                // Set the texture type to Sprite
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                if (textureImporter)
                {
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.SaveAndReimport();

                    return path;
                }
            }

            Debug.LogError("Failed to save icon for " + name);

            return null;
        }

        private void OnReferenceChanged()
        {
            if (icon)
            {
                bool changeIcon = EditorUtility.DisplayDialog("Change Icon?", "The prefab reference has changed, do you want to update the icon?", "Yes", "No");

                if (!changeIcon)
                {
                    return;
                }
            }

            CreateIconAssetContextMenu();
        }
    }
}
#endif