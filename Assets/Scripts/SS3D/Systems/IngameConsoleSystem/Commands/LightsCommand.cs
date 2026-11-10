using System.Collections.Generic;
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
    public class LightsCommand : Command
    {
        private IReadOnlyList<Subcommand> _subcommands;

        public override string Name => "lights";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Switch the lights of the area you're standing in on or off";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override string Example => "off";

        public override IReadOnlyList<Subcommand> Subcommands => _subcommands ??= new[]
        {
            new Subcommand("on", "Switch your area's lights on", (context, _) => Run(context, true)),
            new Subcommand("off", "Switch your area's lights off", (context, _) => Run(context, false)),
        };

        private static CommandResult Run(CommandContext context, bool switchOn)
        {
            Entity entity = context.CallerEntity;
            if (entity == null)
            {
                return CommandResult.Error("you have no body to target from - embark first");
            }

            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            if (tiles?.QueryService == null || tiles.CurrentMap == null || !SubSystems.TryGet(out AreaSubSystem areas))
            {
                return CommandResult.Error("Tile or area system not ready");
            }

            TileCoord coord = tiles.QueryService.WorldToTile(entity.Transform.position, tiles.CurrentMap.MapId);
            if (!areas.TryGetAreaForTile(coord, out AreaRecord area))
            {
                return CommandResult.Error($"No area at {coord.Grid.x},{coord.Grid.y}");
            }

            if (areas.TryGetAreaLightingSwitchOn(area.Id, out bool on) && on != switchOn)
            {
                areas.ToggleAreaLightingSwitch(area.Id);
            }

            return CommandResult.Success($"Lights {(switchOn ? "on" : "off")} in area {area.Id.Value}");
        }
    }
}
