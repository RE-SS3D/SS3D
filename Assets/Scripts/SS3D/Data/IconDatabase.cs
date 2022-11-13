#if UNITY_EDITOR


#endif
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Data
{
    [CreateAssetMenu(menuName = "Database/Icons", fileName = "Icons")]
    public class IconDatabase : ScriptableSettings
    {
        public string EnumName;
        public List<AssetReference> Assets;

        public void PreloadAssets()
        {
            foreach (AssetReference assetReference in Assets)
            {
                assetReference.LoadAssetAsync<Sprite>();
            }
        }

        public Sprite Get(InteractionIcons icon)
        {
            return Assets[(int)icon].Asset as Sprite;
        }

#if UNITY_EDITOR
        public void CreateEnum()
        {
            IEnumerable<string> assets = Assets.Select(reference => reference.SubObjectName);
            string assetPath = AssetData.GetAssetPath(this);

            CodeWriter.WriteEnum(assetPath, EnumName, assets);
        }
#endif
    }
}