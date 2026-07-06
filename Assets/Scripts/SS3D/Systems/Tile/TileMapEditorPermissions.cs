using FishNet.Connection;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Server-side permission checks for TileMap Creator map-editing RPCs.
    /// </summary>
    internal static class TileMapEditorPermissions
    {
        internal const ServerRoleTypes RequiredRole = ServerRoleTypes.Administrator;

        internal static bool TryAuthorize(NetworkConnection conn)
        {
            if (conn == null)
                return false;

            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();
            string ckey = playerSystem.GetCkey(conn);

            if (permissionSystem.IsAtLeast(ckey, RequiredRole))
                return true;

            Log.Warning(typeof(TileMapEditorPermissions),
                "User {ckey} denied map edit — requires {requiredRole}", Logs.ServerOnly, ckey, RequiredRole);
            return false;
        }
    }
}
