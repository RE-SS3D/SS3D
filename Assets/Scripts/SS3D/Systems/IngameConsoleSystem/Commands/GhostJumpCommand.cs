using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.PlayerControl;
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

        public override string ShortDescription => "Ghost: jump to the station or to a player";

        public override string LongDescription =>
            "Only works while you're a ghost. With no arguments, jumps to the station's spawn point. " +
            "\"ghostjump (ckey)\" jumps to that player.";

        public override string Usage => "[ckey]";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(string Ckey) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            EntitySubSystem entities = SubSystems.Get<EntitySubSystem>();
            if (!entities.TryGetOwnedEntity(conn, out Entity self) || self == null
                || !self.TryGetComponent(out HumanoidGhostController ghost))
            {
                return "Only ghosts can jump.";
            }

            Vector3 floorPosition;
            string destination;
            if (values.Ckey == null)
            {
                if (!TryGetStationPosition(out floorPosition))
                {
                    return "This map has no spawn point to jump to.";
                }

                destination = "the station";
            }
            else
            {
                Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(values.Ckey);
                Entity target = player != null ? entities.GetSpawnedEntity(player) : null;
                if (target == null)
                {
                    return $"Player {values.Ckey} has no body or ghost to jump to.";
                }

                if (target == self)
                {
                    return "You're already there.";
                }

                // A ghost target already hovers; a body stands on the deck.
                floorPosition = target.TryGetComponent(out HumanoidGhostController _)
                    ? target.transform.position - Vector3.up * GhostHoverHeight
                    : target.transform.position;
                destination = values.Ckey;
            }

            Vector3 position = floorPosition + Vector3.up * GhostHoverHeight;
            ghost.ServerTeleport(position);
            Log.Information(this, "Ghost {ckey} jumped to {destination}", Logs.ServerOnly,
                SubSystems.Get<PlayerSubSystem>().GetCkey(conn), destination);

            return $"Jumped to {destination}.";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            return args.Length switch
            {
                0 => response.MakeValid(new CalculatedValues(null)),
                1 => response.MakeValid(new CalculatedValues(args[0])),
                _ => response.MakeInvalid("Invalid number of arguments"),
            };
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
