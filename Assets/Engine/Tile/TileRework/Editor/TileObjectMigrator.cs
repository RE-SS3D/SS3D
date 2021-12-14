using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.Editor.TileMapEditor
{
    public class TileObjectMigrator : MonoBehaviour
    {
        // [MenuItem("RE:SS3D Editor Tools/Migrate")]
        public static void Migrate()
        {
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TileObjectSO)));

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                TileObjectSO asset = AssetDatabase.LoadAssetAtPath<TileObjectSO>(assetPath);

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
