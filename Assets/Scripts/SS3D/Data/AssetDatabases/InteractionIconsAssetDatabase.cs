using SS3D.Data.Enums;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Asset database for all the interaction icons
    /// </summary>
    [CreateAssetMenu(menuName = "AssetData/InteractionIcons", fileName = "InteractionIcons")]
    public class InteractionIconsAssetDatabase : GenericAssetDatabase
    {
        /// <inheritdoc />
        public override void PreloadAssets()
        {
            PreloadAssets<Sprite>();
        }

        /// <summary>
        /// Gets the sprite based on the interaction asked for.
        /// </summary>
        /// <param name="icon">The interaction icon as a sprite</param>
        /// <returns></returns>
        public Sprite Get(InteractionIcons icon)
        {
            return Get<Sprite>((int)icon);
        }
    }
}