using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Permissions;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class HealCommand : Command
    {
        private const string AllZones = "all";

        /// <summary>Heals more than any zone can hold, so the default reads as "full".</summary>
        private const float FullHeal = 999f;

        private static readonly IReadOnlyList<CommandParameter> Params = new[]
        {
            P.Target(),
            P.Choice("zone", new[] { AllZones }.Concat(Enum.GetNames(typeof(BodyZone))).ToArray()).Default(AllZones),
            P.Float("brute").Range(0, 1000).Default(FullHeal, "full"),
            P.Float("burn").Range(0, 1000).Default(FullHeal, "full"),
        };

        public override string Name => "heal";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Treat a zone and stop its bleeding; all zones also restores blood and organs";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "me";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            if (!HealthTargets.TryGetHealth(args.Target(), out HumanHealthController health, out CommandResult error))
            {
                return error;
            }

            string zone = args.Text("zone");
            float brute = args.Float("brute");
            float burn = args.Float("burn");

            if (zone == AllZones)
            {
                for (int i = 0; i < HealthConstants.ZoneCount; i++)
                {
                    health.ApplyTreatment((BodyZone)i, brute, burn, stopBleeding: true);
                }

                health.RestoreSystemicPools();
                health.RestoreOrgans();
                return CommandResult.Success("Player fully healed");
            }

            health.ApplyTreatment((BodyZone)Enum.Parse(typeof(BodyZone), zone), brute, burn, stopBleeding: true);
            return CommandResult.Success("Player healed");
        }
    }
}
