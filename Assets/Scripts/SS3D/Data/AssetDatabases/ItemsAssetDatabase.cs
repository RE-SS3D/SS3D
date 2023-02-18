using Coimbra;
using SS3D.Data.Enums;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Asset database for all the items
    /// </summary>
    [ProjectSettings("SS3D/Assets", "Items")]
    public class ItemsAssetDatabase : AssetDatabase
    {
        /// <inheritdoc />
        public override void PreloadAssets()
        {
            PreloadAssets<GameObject>();
        }

        /// <summary>
        /// Gets the sprite based on the interaction asked for.
        /// </summary>
        /// <param name="icon">The interaction icon as a sprite</param>
        /// <returns></returns>
        public GameObject Get(ItemIDs icon)
        {
            return Get<GameObject>((int)icon);
        }
    }
}