using Coimbra;
using UnityEngine;

namespace SS3D.Permissions
{
    /// <summary>
    /// This settings has general options for the permission system, so if we add a database/SQL/API support we can set stuff here.
    /// </summary>
    [ProjectSettings("SS3D/Server")]
    public class PermissionSettings : ScriptableSettings
    {
        [SerializeField]
        private bool _addServerOwnerPermissionToServerHost;

        /// <summary>
        /// We can define if the host will get the owner permission when he joins the game.
        /// </summary>
        public static bool AddServerOwnerPermissionToServerHost => GetOrFind<PermissionSettings>()._addServerOwnerPermissionToServerHost;
    }
}