using System.Collections.Generic;
using FishNet.Object;
using SS3D.Permissions;
using SS3D.Systems.Atmospherics;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Toggles wind on bodies (default on) via <see cref="AtmosWind.BodiesIgnoreWind"/>. Off stops wind
    /// pushing or knocking down humanoids and moving loose items. Does not pause the gas sim or hide
    /// streaks; pair with <c>atmosdamage off</c> to ignore atmosphere entirely.
    /// </summary>
    public class WindCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Choice("mode", "on", "off", "status").Default("status"),
        };

        public override string Name => "wind";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Turn wind pushing players and items on or off, or show whether it is on";
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
                return CommandResult.Success(AtmosWind.BodiesIgnoreWind
                    ? "Wind on bodies: OFF (ignored)"
                    : "Wind on bodies: ON");
            }

            bool ignore = mode == "off";
            AtmosWind.BodiesIgnoreWind = ignore;
            return CommandResult.Success(ignore
                ? "Wind on bodies disabled"
                : "Wind on bodies enabled");
        }
    }
}
