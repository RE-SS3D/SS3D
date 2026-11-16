using System.Collections.Generic;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Entities;
using SS3D.Systems.Tile;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Console equivalent of <see cref="AtmosDebugController"/>'s GUI buttons (add heat/gas,
    /// wake a region) — that panel is OnGUI-only, so it's unreachable on a dedicated server or
    /// in the headless multiplayer test harness. Always targets the calling player's own tile,
    /// since a debug/automation caller has no mouse to click a tile with.
    /// </summary>
    public class AtmosCommand : Command
    {
        private IReadOnlyList<Subcommand> _subcommands;

        public override string Name => "atmos";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Add heat or gas, or wake atmos cells, at your own tile";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override string Example => "gas plasma 10";

        public override IReadOnlyList<Subcommand> Subcommands => _subcommands ??= new[]
        {
            new Subcommand("heat", "Add heat to your tile", PerformHeat, P.Float("kelvin")),
            new Subcommand("gas", "Add moles of a gas to your tile", PerformGas,
                P.Choice("gas", "oxygen", "nitrogen", "carbondioxide", "plasma"), P.Float("moles")),
            new Subcommand("wake", "Wake atmos cells around your tile", PerformWake, P.Int("radius")),
        };

        private static CommandResult PerformHeat(CommandContext context, CommandArgs args)
        {
            if (!TryGetTile(context, out AtmosSubSystem atmos, out TileCoord coord, out CommandResult error))
            {
                return error;
            }

            float deltaKelvin = args.Float("kelvin");
            atmos.DebugAddHeat(coord, deltaKelvin);
            return CommandResult.Success($"Added {deltaKelvin:F0} K at {coord.Grid.x},{coord.Grid.y}");
        }

        private static CommandResult PerformGas(CommandContext context, CommandArgs args)
        {
            if (!TryGetTile(context, out AtmosSubSystem atmos, out TileCoord coord, out CommandResult error))
            {
                return error;
            }

            float moles = args.Float("moles");
            atmos.DebugAddGas(coord, GasFromName(args.Text("gas")), moles);
            return CommandResult.Success($"Added {moles:F1} mol at {coord.Grid.x},{coord.Grid.y}");
        }

        private static CommandResult PerformWake(CommandContext context, CommandArgs args)
        {
            if (!TryGetTile(context, out AtmosSubSystem atmos, out TileCoord coord, out CommandResult error))
            {
                return error;
            }

            int radius = args.Int("radius");
            atmos.DebugWakeRegion(coord, radius);
            return CommandResult.Success($"Woke radius {radius} at {coord.Grid.x},{coord.Grid.y}");
        }

        private static bool TryGetTile(CommandContext context, out AtmosSubSystem atmos, out TileCoord coord, out CommandResult error)
        {
            atmos = null;
            coord = default;
            error = default;

            Entity entity = context.CallerEntity;
            if (entity == null)
            {
                error = CommandResult.Error("you have no body to target from — embark first");
                return false;
            }

            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            atmos = SubSystems.Get<AtmosSubSystem>();
            if (tileSubSystem?.QueryService == null || tileSubSystem.CurrentMap == null || atmos == null)
            {
                error = CommandResult.Error("Atmos or tile system not ready");
                return false;
            }

            coord = tileSubSystem.QueryService.WorldToTile(entity.Transform.position, tileSubSystem.CurrentMap.MapId);
            return true;
        }

        /// <summary>Names match the <c>gas</c> choice list; the registry has already rejected anything else.</summary>
        private static GasId GasFromName(string name)
        {
            return name switch
            {
                "oxygen" => AtmosConstants.Oxygen,
                "nitrogen" => AtmosConstants.Nitrogen,
                "carbondioxide" => AtmosConstants.CarbonDioxide,
                _ => AtmosConstants.Plasma,
            };
        }
    }
}
