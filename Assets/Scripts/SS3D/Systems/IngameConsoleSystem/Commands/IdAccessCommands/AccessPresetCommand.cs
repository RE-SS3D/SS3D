using SS3D.Permissions;
using SS3D.Systems.IdAccess;
using System.Collections.Generic;

namespace SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands
{
    /// <summary>Replaces the target's crew record access with a named preset.</summary>
    public class AccessPresetCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[]
        {
            P.Target(),
            P.Choice("preset", "none", "standard", "engineer", "security", "hop", "captain"),
        };

        public override string Name => "accesspreset";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Set the access on the target's active ID card record from a preset";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "me engineer";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            if (!IdAccessCommandUtilities.TryParsePreset(args.Text("preset"), out AccessMask mask, out string error))
            {
                return CommandResult.Error(error);
            }

            return IdAccessCommandUtilities.UpdateRecordAccess(args.Target(), _ => mask, "Access preset applied");
        }
    }
}
