using System.Collections.Generic;
using SS3D.Permissions;
using SS3D.Systems.Combat;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Client toggle for gold/grey ranged impact spheres (default off). Bullet holes and muzzle flash stay either way.
    /// </summary>
    public class RangedMarkersCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Choice("mode", "on", "off", "status").Default("status"),
        };

        public override string Name => "rangedmarkers";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Show or hide gold/grey spheres at hitscan impact points";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;
        public override IReadOnlyList<CommandParameter> Parameters => Declared;
        public override string Example => "on";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            string mode = args.Text("mode");
            if (mode == "status")
            {
                return CommandResult.Success(RangedShotFeedback.ShowImpactMarkers
                    ? "Ranged impact markers: ON"
                    : "Ranged impact markers: OFF");
            }

            bool enable = mode == "on";
            RangedShotFeedback.ShowImpactMarkers = enable;
            return CommandResult.Success(enable
                ? "Ranged impact markers enabled"
                : "Ranged impact markers disabled");
        }
    }
}
