using System.Collections.Generic;
using SS3D.Permissions;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class KillCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[] { P.Target() };

        public override string Name => "kill";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Kill the target (brain death, so health reflects it)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "nearest";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            CommandTarget target = args.Target();

            // Route through the canonical brain-death trigger (health.md) rather than
            // ghosting the entity directly, so health state reflects the death.
            if (target.TryGetComponent(out HumanHealthController health))
            {
                health.ForceBrainDeath();
            }
            else
            {
                target.Entity.Kill();
            }

            return CommandResult.Success($"Player killed: {target.DisplayName}");
        }
    }
}
