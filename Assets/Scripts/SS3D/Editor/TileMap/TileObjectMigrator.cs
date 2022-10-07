using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.TileMap
{
    public class TileObjectMigrator : MonoBehaviour
    {
        // [MenuItem("RE:SS3D Editor Tools/Migrate")]
        public static void Migrate()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TileObjectSo)}");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TileObjectSo asset = AssetDatabase.LoadAssetAtPath<TileObjectSo>(assetPath);

                if (asset.nameString == asset.prefab.name)
                {
                    continue;
                }

                asset.nameString = asset.prefab.name;

                AssetDatabase.RenameAsset(assetPath, asset.prefab.name);
                AssetDatabase.SaveAssets();

                Debug.Log("Migrated object: " + asset.name + ", New name: " + asset.nameString);
            }
        }
    }
}
