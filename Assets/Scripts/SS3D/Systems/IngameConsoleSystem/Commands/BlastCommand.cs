using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Admin: run blast BFS from the caller's standing tile.
    /// </summary>
    public class BlastCommand : Command
    {
        private const float DefaultYield = 120f;
        private const float DefaultFalloff = 25f;

        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Float("yield").Default(DefaultYield).Range(0.01, 100000),
            P.Float("falloff").Default(DefaultFalloff).Range(0, 100000),
        };

        public override string Name => "blast";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Detonate a hop-based blast at your standing tile";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Declared;
        public override string Example => "200 30";

        [Server]
        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            float yield = args.Float("yield");
            float falloff = args.Float("falloff");

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
            if (structural == null || structural.Blast == null)
            {
                return CommandResult.Error("StructuralDamageSubSystem / blast not ready");
            }

            int mapId = tiles.CurrentMap.MapId;
            TileCoord epicenter = tiles.QueryService.WorldToTile(entity.transform.position, mapId);
            structural.ResolveBlast(epicenter, yield, falloff);

            return CommandResult.Success($"Blast at {epicenter.Grid.x},{epicenter.Grid.y} map {mapId}: yield {yield:0.#}, falloff {falloff:0.#}");
        }
    }
}
