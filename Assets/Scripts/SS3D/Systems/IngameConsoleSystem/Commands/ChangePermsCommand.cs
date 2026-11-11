using System.Collections.Generic;
using SS3D.Core;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>Grant a server role. admin-tools.md §2: only a ServerOwner grants and revokes roles.</summary>
    public class ChangePermsCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[]
        {
            P.Text("ckey"),
            P.Enum("role", ServerRoleTypes.Restricted, ServerRoleTypes.User, ServerRoleTypes.Administrator, ServerRoleTypes.ServerOwner),
        };

        public override string Name => "changeperms";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Set a player's server role";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.ServerOwner;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "editorUser Administrator";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            ServerRoleTypes role = args.Get<ServerRoleTypes>("role");
            SubSystems.Get<PermissionSubSystem>().ChangeUserPermission(args.Text("ckey"), role);
            return CommandResult.Success($"Permission of {args.Text("ckey")} changed to {role}");
        }
    }
}
