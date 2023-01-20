using SS3D.Data.Enums;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Asset database for all the interaction icons
    /// </summary>
    [CreateAssetMenu(menuName = "AssetData/IDPermissions", fileName = "IDPermissions")]
    public class IDPermissionsAssetDatabase : GenericAssetDatabase
    {
        /// <inheritdoc />
        public override void PreloadAssets()
        {
            PreloadAssets<ScriptableObject>();
        }

        /// <summary>
        /// Gets the sprite based on the interaction asked for.
        /// </summary>
        /// <param name="icon">The interaction icon as a sprite</param>
        /// <returns></returns>
        public ScriptableObject Get(AccessPermissionIDs icon)
        {
            return Get<ScriptableObject>((int)icon);
        }
    }
}