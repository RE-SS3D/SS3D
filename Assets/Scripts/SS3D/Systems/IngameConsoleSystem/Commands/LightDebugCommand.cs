using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Area;
using SS3D.Systems.Entities;
using SS3D.Systems.Tile;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Sets the light switch state of the area the caller stands in, through the same path as a
    /// wall switch (<see cref="AreaSubSystem.ToggleAreaLightingSwitch"/>). Lets a headless
    /// automation client change lighting; the observer-sync smoke scenario uses it.
    /// </summary>
    public class LightDebugCommand : Command
    {
        public override string ShortDescription => "Switch the lights of the area you're standing in on or off";
        public override string Usage => "(on | off)\nexample: lightdebug off";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(bool On) : ICalculatedValues;

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

            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            if (tiles?.QueryService == null || tiles.CurrentMap == null || !SubSystems.TryGet(out AreaSubSystem areas))
            {
                return "Tile or area system not ready";
            }

            TileCoord coord = tiles.QueryService.WorldToTile(entity.Transform.position, tiles.CurrentMap.MapId);
            if (!areas.TryGetAreaForTile(coord, out AreaRecord area))
            {
                return $"No area at {coord.Grid.x},{coord.Grid.y}";
            }

            if (areas.TryGetAreaLightingSwitchOn(area.Id, out bool on) && on != values.On)
            {
                areas.ToggleAreaLightingSwitch(area.Id);
            }

            return $"Lights {(values.On ? "on" : "off")} in area {area.Id.Value}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 1 || (args[0] != "on" && args[0] != "off"))
            {
                return response.MakeInvalid(WrongArgsText);
            }

            return response.MakeValid(new CalculatedValues(args[0] == "on"));
        }
    }
}
