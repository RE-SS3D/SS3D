using System.Collections.Generic;
using SS3D.Permissions;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class DefibCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[] { P.Target() };

        public override string Name => "defib";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Defibrillate the target's chest";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "nearest";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            if (!HealthTargets.TryGetHealth(args.Target(), out HumanHealthController health, out CommandResult error))
            {
                return error;
            }

            DefibrillatorOutcome outcome = health.TryDefibrillate(BodyZone.Chest);
            return outcome switch
            {
                DefibrillatorOutcome.Success => CommandResult.Success("Heart restarted"),
                DefibrillatorOutcome.NoResponse => CommandResult.Success("No response (brain dead)"),
                DefibrillatorOutcome.UnnecessaryShock => CommandResult.Success("Unnecessary shock (burn applied)"),
                _ => CommandResult.Error("defibrillation failed"),
            };
        }
    }
}
