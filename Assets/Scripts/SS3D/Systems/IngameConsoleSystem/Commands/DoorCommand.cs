using System.Collections.Generic;
using System.Linq;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Opens (and holds open) or closes the airlock nearest the caller's body, bypassing access.
    /// Lets a headless automation client change door state without walking into the proximity
    /// trigger; the observer-sync smoke scenario uses it.
    /// </summary>
    public class DoorCommand : Command
    {
        private const float MaxDistance = 30f;

        private IReadOnlyList<Subcommand> _subcommands;

        public override string Name => "door";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Open (and hold open) or close the airlock nearest you, bypassing access";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override string Example => "open";

        public override IReadOnlyList<Subcommand> Subcommands => _subcommands ??= new[]
        {
            new Subcommand("open", "Open and hold open the nearest airlock", (context, _) => Run(context, true)),
            new Subcommand("close", "Close the nearest airlock", (context, _) => Run(context, false)),
        };

        private static CommandResult Run(CommandContext context, bool open)
        {
            Entity entity = context.CallerEntity;
            if (entity == null)
            {
                return CommandResult.Error("you have no body to target from - embark first");
            }

            Vector3 position = entity.Transform.position;
            AirLockOpener airlock = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None)
                .OrderBy(candidate => Vector3.Distance(candidate.transform.position, position))
                .FirstOrDefault();
            if (airlock == null || Vector3.Distance(airlock.transform.position, position) > MaxDistance)
            {
                return CommandResult.Error($"No airlock within {MaxDistance:F0} m");
            }

            if (!open)
            {
                airlock.ServerCloseFromInteraction();
                return CommandResult.Success($"Closed {airlock.name}");
            }

            return airlock.ServerDebugHoldOpen()
                ? CommandResult.Success($"Opened {airlock.name}")
                : CommandResult.Error($"{airlock.name} is unpowered");
        }
    }
}
