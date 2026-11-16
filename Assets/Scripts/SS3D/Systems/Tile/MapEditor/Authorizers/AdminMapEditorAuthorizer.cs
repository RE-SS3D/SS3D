using FishNet.Connection;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.Tile.MapEditor.Authorizers
{
    /// <summary>
    /// Requires Administrator server role for map editor access.
    /// </summary>
    public sealed class AdminMapEditorAuthorizer : IMapEditorAuthorizer
    {
        internal const ServerRoleTypes RequiredRole = ServerRoleTypes.Administrator;

        public bool TryAuthorize(NetworkConnection conn)
        {
            if (conn == null)
                return false;

            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();
            string ckey = playerSystem.GetCkey(conn);

            if (permissionSystem.IsAtLeast(ckey, RequiredRole))
                return true;

            Log.Warning(typeof(AdminMapEditorAuthorizer),
                "User {ckey} denied map edit — requires {requiredRole}", Logs.ServerOnly, ckey, RequiredRole);
            return false;
        }
    }
}
