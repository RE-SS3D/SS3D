using SS3D.Core;
using System.Collections.Generic;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Lets a ghost get back to the station, or to another player, without flying there (#151).
    /// Thin stand-in for an observer jump-to menu.
    /// </summary>
    public class GhostJumpCommand : Command
    {
        /// <summary>Ghosts hover about this far above the deck (bug reports show y ≈ 1.06 on a floor at 0).</summary>
        private const float GhostHoverHeight = 1.05f;

        private static readonly IReadOnlyList<CommandParameter> Params = new[] { P.Target().Optional() };

        public override string Name => "ghostjump";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Ghosts only: jump to the station's spawn point, or to the target";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            Entity self = context.CallerEntity;
            if (self == null || !self.TryGetComponent(out HumanoidGhostController ghost))
            {
                return CommandResult.Error("Only ghosts can jump.");
            }

            Vector3 floorPosition;
            string destination;
            if (!args.Has("target"))
            {
                if (!TryGetStationPosition(out floorPosition))
                {
                    return CommandResult.Error("this map has no spawn point to jump to.");
                }

                destination = "the station";
            }
            else
            {
                CommandTarget target = args.Target();
                if (target.Entity == self)
                {
                    return CommandResult.Error("You're already there.");
                }

                // A ghost target already hovers; a body stands on the deck.
                floorPosition = target.TryGetComponent(out HumanoidGhostController _)
                    ? target.Entity.transform.position - Vector3.up * GhostHoverHeight
                    : target.Entity.transform.position;
                destination = target.DisplayName;
            }

            Vector3 position = floorPosition + Vector3.up * GhostHoverHeight;
            ghost.ServerTeleport(position);
            Log.Information(this, "Ghost {ckey} jumped to {destination}", Logs.ServerOnly, context.Ckey, destination);

            return CommandResult.Success($"Jumped to {destination}.");
        }

        private static bool TryGetStationPosition(out Vector3 position)
        {
            if (SubSystems.TryGet(out TileSubSystem tile)
                && tile.SpawnPoints != null
                && tile.SpawnPoints.Count > 0)
            {
                position = tile.SpawnPoints.Records[0].Position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
