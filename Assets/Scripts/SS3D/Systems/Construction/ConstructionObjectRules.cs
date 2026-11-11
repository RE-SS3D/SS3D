using System;
using SS3D.Core;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Which placed tile objects an object step (weld, pry, repair, construct a girder) acts on. Shared by
    /// discovery, the hover outline, <c>CanInteract</c> and the server's completion check.
    /// </summary>
    public static class ConstructionObjectRules
    {
        public static bool IsDamaged(PlacedTileObject placed) =>
            placed.IntegrityStage is StructuralIntegrityStage.Damaged or StructuralIntegrityStage.Cracked;

        /// <summary>Whether <paramref name="placed"/> is the kind of object <paramref name="rule"/> acts on.</summary>
        /// <remarks>Cell steps (<see cref="ConstructionRule.ActsOnCell"/>) read the current map from <see cref="TileSubSystem"/>.</remarks>
        public static bool Matches(ConstructionRule rule, PlacedTileObject placed)
        {
            if (rule != null && rule.ActsOnCell)
            {
                TileSubSystem tiles = ResolveTiles();
                return Matches(rule, placed, tiles != null ? tiles.CurrentMap : null, tiles != null ? tiles.HasWreckage : null);
            }

            return Matches(rule, placed, null, null);
        }

        /// <summary><see cref="Matches(ConstructionRule, PlacedTileObject)"/> against an explicit map (EditMode tests).</summary>
        public static bool Matches(ConstructionRule rule, PlacedTileObject placed, TileMap map,
            Func<TileCoord, TileLayer, bool> hasWreckage)
        {
            if (rule == null || rule.IsCellAction || placed == null || placed.tileObjectSO == null)
            {
                return false;
            }

            if (rule.ActsOnCell)
            {
                return IsCellHost(rule, placed)
                    && ConstructionCellRules.Matches(map, rule, ConstructionApply.GridWorld(placed), hasWreckage);
            }

            if (placed.Layer != rule.Layer)
            {
                return false;
            }

            return rule.Target switch
            {
                ConstructionTargetKind.FloorTurf => placed.GenericType == TileObjectGenericType.Floor,
                ConstructionTargetKind.DamagedPlenum => placed.GenericType == TileObjectGenericType.Plenum && IsDamaged(placed),
                ConstructionTargetKind.DamagedStructure => placed.GenericType == TileObjectGenericType.Wall && IsDamaged(placed),
                ConstructionTargetKind.NamedTile => placed.NameString == rule.TargetName
                    && (!rule.RequiresIntact || !IsDamaged(placed))
                    && (!rule.RefusesReinforced || !placed.tileObjectSO.reinforced),
                _ => false,
            };
        }

        /// <summary>
        /// Why a matched cell step cannot run on <paramref name="placed"/>'s cell (wreckage in the way,
        /// furniture), or null when it can. Object steps that are not cell steps always pass.
        /// </summary>
        public static string CellFailure(ConstructionRule rule, PlacedTileObject placed)
        {
            if (rule == null || !rule.ActsOnCell || placed == null)
            {
                return null;
            }

            TileSubSystem tiles = ResolveTiles();
            if (tiles == null || tiles.CurrentMap == null)
            {
                return "No map";
            }

            TileObjectSo result = string.IsNullOrEmpty(rule.ResultName) ? null : tiles.GetAsset(rule.ResultName) as TileObjectSo;
            return ConstructionCellRules.Validate(tiles.CurrentMap, rule, ConstructionApply.GridWorld(placed), result,
                checkStation: false, tiles.HasWreckage);
        }

        /// <summary>
        /// The hovered object a cell step is offered on: the plating for a floor tile or wreckage, the floor
        /// tile or bare plating for a girder.
        /// </summary>
        private static bool IsCellHost(ConstructionRule rule, PlacedTileObject placed)
        {
            bool plating = placed.Layer == TileLayer.Plenum && placed.GenericType == TileObjectGenericType.Plenum
                && placed.NameString.Contains(ConstructionNames.Plenum);
            bool floor = placed.Layer == TileLayer.Turf && placed.GenericType == TileObjectGenericType.Floor;

            return rule.Target switch
            {
                ConstructionTargetKind.BarePlating => plating,
                ConstructionTargetKind.ClearFloor or ConstructionTargetKind.Wreckage => plating || floor,
                _ => false,
            };
        }

        private static TileSubSystem ResolveTiles() =>
            UnityEngine.Application.isPlaying && SubSystems.TryGet(out TileSubSystem tiles) ? tiles : null;
    }
}
