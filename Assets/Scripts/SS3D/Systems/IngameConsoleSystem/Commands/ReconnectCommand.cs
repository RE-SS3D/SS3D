using System.Diagnostics;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class ReconnectCommand : Command
    {
        public override string Name => "reconnect";
        public override CommandCategory Category => CommandCategory.Session;
        public override string Description => "Restart the game.";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Local;

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            Process.Start(UnityEngine.Application.dataPath.Replace("_Data", ".exe"));
            UnityEngine.Application.Quit();
            return CommandResult.Success("Done");
        }
    }
}
