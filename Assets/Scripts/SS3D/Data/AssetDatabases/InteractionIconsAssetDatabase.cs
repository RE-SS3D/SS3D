using SS3D.Data.Enums;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace SS3D.Data.AssetDatabases
{
    [CreateAssetMenu(menuName = "AssetData/InteractionIcons", fileName = "InteractionIcons")]
    public class InteractionIconsAssetDatabase : GenericAssetDatabase
    {
        public override void PreloadAssets()
        {
            PreloadAssets<Sprite>();
        }

        public Sprite Get(InteractionIcons icon)
        {
            return Get<Sprite>((int)icon);
        }
    }
}