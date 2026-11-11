using System.Collections.Generic;
using SS3D.Permissions;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class SeverCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[]
        {
            P.Target(),
            P.Enum<BodyZone>("zone"),
            P.Choice("mode", "force").Optional(),
        };

        public override string Name => "sever";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Sever a limb or the head; needs the zone Disabled unless force";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "nearest Head force";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            if (!HealthTargets.TryGetHealth(args.Target(), out HumanHealthController health, out CommandResult error))
            {
                return error;
            }

            BodyZone zone = args.Get<BodyZone>("zone");
            if (!health.TrySeverZone(zone, args.Has("mode")))
            {
                return CommandResult.Error("severance failed: zone not severable, already severed, or not Disabled yet");
            }

            return CommandResult.Success($"Severed {zone}");
        }
    }
}
