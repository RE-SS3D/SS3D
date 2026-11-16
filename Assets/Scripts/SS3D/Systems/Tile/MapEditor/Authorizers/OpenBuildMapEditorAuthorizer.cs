using FishNet.Connection;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.Tile.MapEditor.Authorizers
{
    /// <summary>
    /// Sandbox servers: every logged-in player may use the map editor, without becoming an admin.
    /// Only players with the Restricted role are refused. Used only on a server launched with
    /// <c>-sandbox</c> (<see cref="MapEditorPermissions.CreateDefault"/>); round controls and other admin
    /// tools stay admin-only.
    /// </summary>
    public sealed class OpenBuildMapEditorAuthorizer : IMapEditorAuthorizer
    {
        public bool TryAuthorize(NetworkConnection conn)
        {
            if (conn == null)
                return false;

            string ckey = SubSystems.Get<PlayerSubSystem>().GetCkey(conn);
            if (string.IsNullOrEmpty(ckey))
                return false;

            if (SubSystems.Get<PermissionSubSystem>().TryGetUserRole(ckey, out ServerRoleTypes role)
                && role == ServerRoleTypes.Restricted)
            {
                Log.Warning(typeof(OpenBuildMapEditorAuthorizer), "Restricted user {ckey} denied map edit", Logs.ServerOnly, ckey);
                return false;
            }

            return true;
        }
    }
}
