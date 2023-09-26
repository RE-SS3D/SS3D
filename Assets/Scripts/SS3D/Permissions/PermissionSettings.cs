using Coimbra;
using UnityEngine;

namespace SS3D.Permissions
{
    [ProjectSettings("SS3D/Server")]
    public class PermissionSettings : ScriptableSettings
    {
        [SerializeField]
        private bool _addServerOwnerPermissionToServerHost;

        public static bool AddServerOwnerPermissionToServerHost => GetOrFind<PermissionSettings>()._addServerOwnerPermissionToServerHost;
    }
}