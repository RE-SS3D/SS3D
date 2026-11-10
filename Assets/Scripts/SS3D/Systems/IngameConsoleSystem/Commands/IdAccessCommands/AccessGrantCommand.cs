using SS3D.Permissions;
using SS3D.Systems.IdAccess;
using System.Collections.Generic;

namespace SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands
{
    /// <summary>Adds one access level to the crew record bound to the target's active ID card.</summary>
    public class AccessGrantCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[] { P.Target(), P.Text("level") };

        public override string Name => "accessgrant";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Grant an access level to the record on the target's active ID card";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "me Security";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            if (!IdAccessCommandUtilities.TryParseAccessLevel(args.Text("level"), out AccessLevel level, out string error))
            {
                return CommandResult.Error(error);
            }

            return IdAccessCommandUtilities.UpdateRecordAccess(args.Target(), access => access.With(level), $"Granted {level}");
        }
    }
}
