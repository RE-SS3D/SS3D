using System.Collections.Generic;
using System.Linq;
using Coimbra;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Data
{
    [CreateAssetMenu(menuName = "Database/TileObjects", fileName = "TileObjects")]
    public class TileObjectDatabase : ScriptableSettings
    {
        public string EnumName;
        public List<AssetReference> Assets;

        public void PreloadAssets()
        {
            foreach (AssetReference assetReference in Assets)
            {
                assetReference.LoadAssetAsync<GameObject>();
            }
        }

        public GameObject Get(TileObjects tileObject)
        {
            return Assets[(int)tileObject].Asset as GameObject;
        }

#if UNITY_EDITOR
        public void CreateEnum()
        {
            IEnumerable<string> assets = Assets.Select(reference => reference.editorAsset.name);
            string assetPath = AssetData.GetAssetPath(this);

            CodeWriter.WriteEnum(assetPath, EnumName, assets);
        }
#endif
    }
}