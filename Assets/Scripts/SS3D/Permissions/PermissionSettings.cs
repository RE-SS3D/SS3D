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

        [SerializeField]
        private bool _adminFunctionsForAll;

        /// <summary>
        /// We can define if the host will get the owner permission when he joins the game.
        /// </summary>
        public static bool AddServerOwnerPermissionToServerHost => GetOrFind<PermissionSettings>()._addServerOwnerPermissionToServerHost;

        /// <summary>
        /// If true, admin-only functions are available to all users.
        /// Defaults to false, meaning only admins (host by default) can use admin functions.
        /// </summary>
        public static bool AdminFunctionsForAll => GetOrFind<PermissionSettings>()._adminFunctionsForAll;
    }
}