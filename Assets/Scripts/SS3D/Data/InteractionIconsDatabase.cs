using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coimbra;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Data
{
    [CreateAssetMenu(menuName = "AssetData/InteractionIcons", fileName = "InteractionIcons")]
    public class  InteractionIconsDatabase : ScriptableSettings
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
    }
}