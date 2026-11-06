using System;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    public sealed partial class StructuralDamageService
    {
        private static readonly TileLayer[] AllLayers = (TileLayer[])Enum.GetValues(typeof(TileLayer));
        private static readonly Direction[] AllDirections = (Direction[])Enum.GetValues(typeof(Direction));

        /// <summary>Called after blast traversal, so a new hull hole cannot erase the wave's path.</summary>
        public void ApplyBlastFloorDamage(TileCoord coord, float force)
        {
            if (_query == null || _construction == null || !float.IsFinite(force) || force <= 0f) return;
            DamageFloorLayer(coord, TileLayer.Turf, force);
            DamageFloorLayer(coord, TileLayer.Plenum, force);
        }

        private void DamageFloorLayer(TileCoord coord, TileLayer layer, float force)
        {
            if (!_query.TryGetOccupant(coord, layer, Direction.North, out ITileOccupant occupant)
                || occupant is not PlacedTileObject placed || !StructuralIntegrityRules.IsBlastFloor(placed.tileObjectSO)) return;
            float max = StructuralIntegrityRules.ResolveMaxIntegrity(placed.tileObjectSO);
            EnsureInitialized(placed, max);
            float remaining = Mathf.Max(0f, placed.IntegrityRemaining - force);
            StructuralIntegrityStage stage = StructuralIntegrityRules.StageFromRemaining(remaining, max);
            placed.ServerSetIntegrity(remaining, stage);
            if (stage != StructuralIntegrityStage.Destroyed) return;

            // Plenum removal also destroys unsupported machines/storage: release items first.
            if (layer == TileLayer.Plenum) ReleaseUnsupportedContents(coord);
            Vector3 world = _query.TileToWorld(coord);
            StructuralMaterial material = StructuralMaterials.Resolve(placed.tileObjectSO);
            ClearResult result = _construction.TryClearTile(world, layer, placed.Direction);
            if (result.Success && SubSystems.TryGet(out TileSubSystem tiles) && tiles.IsServer)
                tiles.ServerNotifyStructuralImpact(world + Vector3.up * 0.05f, Vector3.zero,
                    material, StructuralImpactCue.Break);
        }

        private void ReleaseUnsupportedContents(TileCoord coord)
        {
            HashSet<PlacedTileObject> seen = new();
            foreach (TileLayer layer in AllLayers)
                foreach (Direction direction in AllDirections)
                {
                    if (!_query.TryGetOccupant(coord, layer, direction, out ITileOccupant occupant)
                        || occupant is not PlacedTileObject placed || !seen.Add(placed)) continue;
                    foreach (AttachedContainer container in placed.GetComponentsInChildren<AttachedContainer>(true))
                        if (container.GetComponentInParent<Item>() == null) container.ReleaseForDestruction();
                }
        }
    }
}
