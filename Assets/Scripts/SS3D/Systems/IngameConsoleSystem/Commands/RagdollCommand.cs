using System.Collections.Generic;
using SS3D.Permissions;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class RagdollCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Params = new[]
        {
            P.Target(),
            P.Float("seconds").Range(0.1, 600).Optional(),
        };

        public override string Name => "ragdoll";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Knock the target down for some seconds, or toggle ragdoll with no time";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "me 2.5";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            if (!args.Target().TryGetComponent(out Ragdoll ragdoll))
            {
                return CommandResult.Error($"{args.Target().DisplayName} can't ragdoll");
            }

            if (args.Has("seconds"))
            {
                ragdoll.ServerKnockdown(args.Float("seconds"));
            }
            else if (ragdoll.IsKnockedDown)
            {
                ragdoll.ServerRecover();
            }
            else
            {
                ragdoll.ServerKnockdownTimeless();
            }

            return CommandResult.Success("Player ragdolled");
        }
    }
}
