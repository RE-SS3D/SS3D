#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ss3dAssetDatabase = SS3D.Data.AssetDatabases.AssetDatabase;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

namespace SS3D.Editor.Interactions
{
    /// <summary>
    /// Rebuilds NativeFormat interaction-icon <c>.asset</c> sprites from their PNG sources and
    /// registers them on the InteractionIcons asset database.
    /// <see cref="Sprite.Create"/> assets often fail to load (null in AssetDatabase) — clone the
    /// texture's imported sprite instead.
    /// </summary>
    public static class InteractionIconSpriteBuilder
    {
        private const string IconsRoot = "Assets/Art/Graphics/UI/Interactions/InteractionIcons";
        private const string SpriteRoot = "Assets/Content/Systems/UI/Systems/Interactions/InteractionIcons";
        private const string InteractionIconsDatabasePath = "Assets/Content/Data/Databases/InteractionIcons.asset";

        [MenuItem("SS3D/Interactions/Rebuild Interaction Icon Sprites")]
        public static void RebuildAllMenu()
        {
            int rebuilt = RebuildAll();
            int registered = RegisterSpritesWithDatabase();
            Debug.Log($"[SS3D] Rebuilt {rebuilt} interaction icon sprite(s); registered {registered} on InteractionIcons.");
        }

        /// <summary>
        /// Rebuilds every PNG under the icons folder into a matching NativeFormat sprite asset,
        /// preserving existing .asset GUIDs when present.
        /// </summary>
        public static int RebuildAll()
        {
            string[] pngGuids = UnityAssetDatabase.FindAssets("t:Texture2D", new[] { IconsRoot });
            int count = 0;

            foreach (string pngGuid in pngGuids)
            {
                string pngPath = UnityAssetDatabase.GUIDToAssetPath(pngGuid);
                if (!pngPath.EndsWith(".png"))
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(pngPath);
                string spritePath = $"{SpriteRoot}/{name}.asset";
                if (RebuildOne(pngPath, spritePath))
                {
                    count++;
                }
            }

            UnityAssetDatabase.SaveAssets();
            UnityAssetDatabase.Refresh();
            return count;
        }

        public static bool RebuildOne(string texturePath, string spriteAssetPath)
        {
            EnsureSpriteImporter(texturePath);

            Sprite source = UnityAssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().FirstOrDefault();
            if (source == null)
            {
                Debug.LogWarning($"[SS3D] No sprite sub-asset on {texturePath}; skip.");
                return false;
            }

            string metaPath = spriteAssetPath + ".meta";
            string preservedMeta = File.Exists(metaPath) ? File.ReadAllText(metaPath) : null;

            if (UnityAssetDatabase.LoadMainAssetAtPath(spriteAssetPath) != null)
            {
                UnityAssetDatabase.DeleteAsset(spriteAssetPath);
            }

            Sprite copy = Object.Instantiate(source);
            copy.name = Path.GetFileNameWithoutExtension(spriteAssetPath);
            UnityAssetDatabase.CreateAsset(copy, spriteAssetPath);

            if (!string.IsNullOrEmpty(preservedMeta))
            {
                File.WriteAllText(metaPath, preservedMeta);
                UnityAssetDatabase.ImportAsset(spriteAssetPath);
            }

            return true;
        }

        private static int RegisterSpritesWithDatabase()
        {
            Ss3dAssetDatabase database = UnityAssetDatabase.LoadAssetAtPath<Ss3dAssetDatabase>(InteractionIconsDatabasePath);
            if (database == null)
            {
                Debug.LogError($"[SS3D] Missing InteractionIcons database at {InteractionIconsDatabasePath}.");
                return 0;
            }

            string[] spriteGuids = UnityAssetDatabase.FindAssets("t:Sprite", new[] { SpriteRoot });
            int registered = 0;

            foreach (string spriteGuid in spriteGuids)
            {
                string path = UnityAssetDatabase.GUIDToAssetPath(spriteGuid);
                if (!path.EndsWith(".asset"))
                {
                    continue;
                }

                Sprite sprite = UnityAssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                if (database.AddToAddressables(sprite))
                {
                    registered++;
                }
            }

            database.LoadAssetsFromAssetGroup();
            database.GenerateDatabaseCode();
            EditorUtility.SetDirty(database);
            UnityAssetDatabase.SaveAssets();
            return registered;
        }

        private static void EnsureSpriteImporter(string texturePath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
