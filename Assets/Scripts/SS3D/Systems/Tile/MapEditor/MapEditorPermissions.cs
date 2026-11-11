using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using System;
using System.Collections.Generic;
using SS3D.Systems.Tile.MapEditor.Authorizers;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Server-side permission checks for map editor RPCs.
    /// </summary>
    public static class MapEditorPermissions
    {
        private static IMapEditorAuthorizer _authorizer = CreateDefault(Environment.GetCommandLineArgs());

        /// <summary>
        /// Override the default authorizer (e.g. creative-mode Builder role check).
        /// </summary>
        public static void SetAuthorizer(IMapEditorAuthorizer authorizer) =>
            _authorizer = authorizer ?? CreateDefault(Environment.GetCommandLineArgs());

        /// <summary>
        /// Admin-only, unless the server was launched with <c>-sandbox</c> (sandbox-server §5), the one mode where
        /// every non-Restricted player may build.
        /// </summary>
        public static IMapEditorAuthorizer CreateDefault(string[] commandLineArgs) =>
            SandboxProfile.RequestedIn(commandLineArgs)
                ? new OpenBuildMapEditorAuthorizer()
                : new AdminMapEditorAuthorizer();

        public static bool TryAuthorize(NetworkConnection conn) => _authorizer.TryAuthorize(conn);

        /// <summary>
        /// Deleting or overwriting map files on the server's disk. Always true with the default admin
        /// authorizer; on the sandbox, only admins, or the player who saved that map this session.
        /// </summary>
        public static bool MayReplaceMapFile(NetworkConnection conn, string mapName)
        {
            if (_authorizer is not OpenBuildMapEditorAuthorizer)
                return true;

            string ckey = SubSystems.Get<PlayerSubSystem>().GetCkey(conn);
            if (SubSystems.Get<PermissionSubSystem>().IsAtLeast(ckey, AdminMapEditorAuthorizer.RequiredRole))
                return true;

            return !string.IsNullOrEmpty(ckey)
                && SavedBy.TryGetValue(mapName, out string owner)
                && owner == ckey;
        }

        /// <summary>
        /// A non-admin editing on the sandbox: throttled, and limited to undoing their own edits.
        /// Always false in the default admin-only mode.
        /// </summary>
        public static bool IsSandboxPlayer(NetworkConnection conn) =>
            _authorizer is OpenBuildMapEditorAuthorizer
            && !conn.IsHost
            && !SubSystems.Get<PermissionSubSystem>().IsAtLeast(
                SubSystems.Get<PlayerSubSystem>().GetCkey(conn), AdminMapEditorAuthorizer.RequiredRole);

        /// <summary>
        /// Map files and the station-replacing editor actions: Save, Export, Load, Delete, New Map and the
        /// round-end autosave. Off on the sandbox, for everyone. Server only; set from
        /// <see cref="ServerSettings.MapPersistence"/>.
        /// </summary>
        public static bool PersistenceAllowed { get; set; } = true;

        public const string PersistenceRefusedMessage = "Saving and loading maps is off on this server. Builds reset with the round.";

        /// <summary>Server-side gate for every persistence path; <paramref name="refusal"/> is the player-facing reason.</summary>
        public static bool TryAllowPersistence(out string refusal)
        {
            refusal = PersistenceAllowed ? null : PersistenceRefusedMessage;
            return PersistenceAllowed;
        }

        /// <summary>Sandbox players can't make themselves invincible; admins and the host can (sandbox-server.md §5).</summary>
        public static bool MayToggleInvincibility(bool isSandboxPlayer) => !isSandboxPlayer;

        public static bool MayToggleInvincibility(NetworkConnection conn) => MayToggleInvincibility(IsSandboxPlayer(conn));

        /// <summary>What this connection's editor may show; sent to the client when the editor opens.</summary>
        public static MapEditorCapabilities CapabilitiesFor(NetworkConnection conn) =>
            MapEditorCapabilities.For(PersistenceAllowed, IsSandboxPlayer(conn));

        /// <summary>Records who saved a map, for <see cref="MayReplaceMapFile"/>. Server only, this session.</summary>
        public static void RecordSave(NetworkConnection conn, string mapName)
        {
            string ckey = SubSystems.Get<PlayerSubSystem>().GetCkey(conn);
            if (!string.IsNullOrEmpty(ckey) && !SavedBy.ContainsKey(mapName))
                SavedBy[mapName] = ckey;
        }

        private static readonly Dictionary<string, string> SavedBy = new(StringComparer.OrdinalIgnoreCase);
    }
}
