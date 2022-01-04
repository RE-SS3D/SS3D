using SS3D.Engine.Tile.TileRework;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.TileMap
{
    public class TileObjectMigrator : MonoBehaviour
    {
        // [MenuItem("RE:SS3D Editor Tools/Migrate")]
        public static void Migrate()
        {
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TileObjectSo)));

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                TileObjectSo asset = AssetDatabase.LoadAssetAtPath<TileObjectSo>(assetPath);

                if (asset.nameString == asset.prefab.name)
                    continue;

                asset.nameString = asset.prefab.name;

                AssetDatabase.RenameAsset(assetPath, asset.prefab.name);
                AssetDatabase.SaveAssets();

                Debug.Log("Migrated object: " + asset.name + ", New name: " + asset.nameString);
            }
        }
    }
}
