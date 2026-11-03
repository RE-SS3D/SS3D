using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Opens (and holds open) or closes the airlock nearest the caller's body, bypassing access.
    /// Lets a headless automation client change door state without walking into the proximity
    /// trigger; the observer-sync smoke scenario uses it.
    /// </summary>
    public class DoorDebugCommand : Command
    {
        private const float MaxDistance = 30f;

        public override string ShortDescription => "Open or close the airlock nearest you";
        public override string Usage => "(open | close)\nexample: doordebug open";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(bool Open) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            if (!SubSystems.Get<EntitySubSystem>().TryGetSpawnedEntity(conn, out Entity entity))
            {
                return "You have no spawned entity to target from - embark first";
            }

            Vector3 position = entity.Transform.position;
            AirLockOpener airlock = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None)
                .OrderBy(candidate => Vector3.Distance(candidate.transform.position, position))
                .FirstOrDefault();
            if (airlock == null || Vector3.Distance(airlock.transform.position, position) > MaxDistance)
            {
                return $"No airlock within {MaxDistance:F0} m";
            }

            if (!values.Open)
            {
                airlock.ServerCloseFromInteraction();
                return $"Closed {airlock.name}";
            }

            return airlock.ServerDebugHoldOpen() ? $"Opened {airlock.name}" : $"{airlock.name} is unpowered";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 1 || (args[0] != "open" && args[0] != "close"))
            {
                return response.MakeInvalid(WrongArgsText);
            }

            return response.MakeValid(new CalculatedValues(args[0] == "open"));
        }
    }
}
