using SS3D.Permissions;
using SS3D.Systems.IdAccess;
using System.Collections.Generic;

namespace SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands
{
    /// <summary>
    /// Lists every ID card on the target, which one the access resolver picks, the bound record's
    /// access, and optionally whether it passes a level.
    /// </summary>
    public class AccessCheckCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[] { P.Target(), P.Text("level").Optional() };

        public override string Name => "accesscheck";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Show the target's ID cards and record access, optionally testing a level";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "me Engineering";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            AccessLevel? testLevel = null;
            if (args.Has("level"))
            {
                if (!IdAccessCommandUtilities.TryParseAccessLevel(args.Text("level"), out AccessLevel level, out string levelError))
                {
                    return CommandResult.Error(levelError);
                }

                testLevel = level;
            }

            if (!IdAccessCommandUtilities.TryGetInventory(args.Target(), out var inventory, out string error))
            {
                return CommandResult.Error(error);
            }

            return CommandResult.Success(IdAccessCommandUtilities.BuildCredentialReport(inventory, testLevel));
        }
    }
}
