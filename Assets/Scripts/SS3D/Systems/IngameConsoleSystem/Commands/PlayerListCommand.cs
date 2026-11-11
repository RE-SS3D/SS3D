using System.Linq;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Lists online ckeys from this client's replicated player list. Client type on purpose: the
    /// multiplayer late-join smoke uses it as a client→server→client round trip.
    /// </summary>
    public class PlayerListCommand : Command
    {
        public override string Name => "playerlist";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Show every player online";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;
        public override string Example => "";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            string players = string.Join("\t", SubSystems.Get<PlayerSubSystem>().OnlinePlayers.Select(p => p.Ckey));
            return CommandResult.Success(players);
        }
    }
}
