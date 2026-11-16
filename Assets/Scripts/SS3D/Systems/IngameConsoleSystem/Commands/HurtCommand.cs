using System.Collections.Generic;
using SS3D.Permissions;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class HurtCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[]
        {
            P.Target(),
            P.Enum<BodyZone>("zone"),
            P.Float("brute").Range(0, 1000),
            P.Float("burn").Range(0, 1000).Default(0f),
        };

        public override string Name => "hurt";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Apply brute and burn damage to one body zone";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "me Chest 25 0";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            if (!HealthTargets.TryGetHealth(args.Target(), out HumanHealthController health, out CommandResult error))
            {
                return error;
            }

            health.ApplyDamage(args.Get<BodyZone>("zone"), args.Float("brute"), args.Float("burn"));
            return CommandResult.Success($"Player hurt: {args.Target().DisplayName}");
        }
    }
}
