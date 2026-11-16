using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Admin: apply structural force to the Turf wall/door on the tile in front of the caller.
    /// </summary>
    public class HurtStructureCommand : Command
    {
        private const float DefaultForce = 40f;

        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Float("force").Default(DefaultForce).Range(0.01, 100000),
        };

        public override string Name => "hurtstructure";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Apply structural damage to the wall, door or window on the tile ahead of you";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Declared;
        public override string Example => "100";

        [Server]
        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            float force = args.Float("force");

            Entity entity = context.CallerEntity;
            if (entity == null)
            {
                return CommandResult.Error("you have no body");
            }

            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            if (tiles == null || tiles.CurrentMap == null || tiles.QueryService == null)
            {
                return CommandResult.Error("TileSubSystem / map not ready");
            }

            StructuralDamageSubSystem structural = SubSystems.Get<StructuralDamageSubSystem>();
            if (structural == null)
            {
                return CommandResult.Error("StructuralDamageSubSystem not registered");
            }

            // Standing-tile + one cardinal step ahead — not world-position + forward*distance,
            // which often rounds back onto the floor underfoot or overshoots past the wall.
            int mapId = tiles.CurrentMap.MapId;
            TileCoord standing = tiles.QueryService.WorldToTile(entity.transform.position, mapId);
            Direction facing = CardinalFromForward(entity.transform.forward);
            TileCoord ahead = OffsetCardinal(standing, facing);

            if (!structural.TryApplyStructuralDamage(ahead, force, StructuralDamageSource.Console))
            {
                return CommandResult.Error($"No structural turf ahead at {ahead.Grid.x},{ahead.Grid.y} map {mapId} (standing {standing.Grid.x},{standing.Grid.y} facing {facing})");
            }

            if (!structural.TryGetIntegrity(ahead, out StructuralIntegrityStage stage, out float remaining, out float max))
            {
                return CommandResult.Success($"Structural Destroyed at {ahead.Grid.x},{ahead.Grid.y}");
            }

            return CommandResult.Success($"Structure at {ahead.Grid.x},{ahead.Grid.y}: {stage} ({remaining:0.#}/{max:0.#})");
        }

        private static Direction CardinalFromForward(Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return Direction.North;
            }

            forward.Normalize();
            if (Mathf.Abs(forward.z) >= Mathf.Abs(forward.x))
            {
                return forward.z >= 0f ? Direction.North : Direction.South;
            }

            return forward.x >= 0f ? Direction.East : Direction.West;
        }

        private static TileCoord OffsetCardinal(TileCoord coord, Direction direction)
        {
            Tuple<int, int> offset = TileHelper.ToCardinalVector(direction);
            return new TileCoord(coord.MapId, coord.Grid.x + offset.Item1, coord.Grid.y + offset.Item2);
        }
    }
}
