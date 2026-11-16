using System.Collections.Generic;
using FishNet.Object;
using SS3D.Permissions;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Toggles turf atmosphere → health damage/oxy/alerts (default on). Does not pause the gas sim.
    /// Off skips turf oxy shortfall, breath exchange, environmental burns and atmos alerts/screens.
    /// </summary>
    public class AtmosDamageCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Choice("mode", "on", "off", "status").Default("status"),
        };

        public override string Name => "atmosdamage";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Turn atmospheric damage on players on or off, or show whether it is on";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Declared;
        public override string Example => "off";

        [Server]
        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            string mode = args.Text("mode");
            if (mode == "status")
            {
                return CommandResult.Success(HealthEnvironmentSettings.AtmosphericDamageDisabled
                    ? "Atmospheric damage: OFF (disabled)"
                    : "Atmospheric damage: ON");
            }

            bool disable = mode == "off";
            HealthEnvironmentSettings.AtmosphericDamageDisabled = disable;
            return CommandResult.Success(disable
                ? "Atmospheric damage disabled"
                : "Atmospheric damage enabled");
        }
    }
}
