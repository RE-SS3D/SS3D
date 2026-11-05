using SS3D.Logging;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Applies per-tile structural integrity damage and clears Destroyed turf.
    /// </summary>
    public sealed class StructuralDamageService : IStructuralDamageService
    {
        private readonly ITileQueryService _query;
        private readonly IConstructionService _construction;

        public StructuralDamageService(ITileQueryService query, IConstructionService construction)
        {
            _query = query;
            _construction = construction;
        }

        public bool TryGetIntegrity(TileCoord coord, out StructuralIntegrityStage stage, out float remaining, out float max)
        {
            stage = StructuralIntegrityStage.Intact;
            remaining = 0f;
            max = 0f;

            if (!TryGetStructural(coord, out PlacedTileObject placed))
                return false;

            max = StructuralIntegrityRules.ResolveMaxIntegrity(placed.tileObjectSO);
            EnsureInitialized(placed, max);
            stage = placed.IntegrityStage;
            remaining = placed.IntegrityRemaining;
            return true;
        }

        public bool TryApplyStructuralDamage(TileCoord coord, float force, StructuralDamageSource source)
        {
            if (force <= 0f || !TryGetStructural(coord, out PlacedTileObject placed))
                return false;

            float max = StructuralIntegrityRules.ResolveMaxIntegrity(placed.tileObjectSO);
            EnsureInitialized(placed, max);

            float remaining = Mathf.Max(0f, placed.IntegrityRemaining - force);
            StructuralIntegrityStage previous = placed.IntegrityStage;
            StructuralIntegrityStage stage = StructuralIntegrityRules.StageFromRemaining(remaining, max);

            if (stage == StructuralIntegrityStage.Destroyed)
            {
                if (placed.Layer == TileLayer.FurnitureBase)
                {
                    foreach (var container in placed.GetComponentsInChildren<SS3D.Systems.Inventory.Containers.AttachedContainer>(true))
                    {
                        // A stored backpack's contents belong to the backpack, not to this casing.
                        if (container.GetComponentInParent<SS3D.Systems.Inventory.Items.Item>() == null)
                            container.ReleaseForDestruction();
                    }
                }
                SteelWreckageRecord wreckage = SteelWreckageRecord.Capture(placed, coord);
                StructuralMaterial material = StructuralMaterials.Resolve(placed.tileObjectSO);
                Vector3 breakPoint = placed.transform.position + Vector3.up;
                placed.ServerSetIntegrity(0f, StructuralIntegrityStage.Destroyed);
                Vector3 world = _query.TileToWorld(coord);
                ClearResult clear = _construction.TryClearTile(world, placed.Layer, placed.Direction);
                if (!clear.Success)
                {
                    Log.Warning(nameof(StructuralDamageService),
                        "Destroyed structural tile at {coord} but TryClearTile failed",
                        Logs.Generic, coord);
                }

                if (clear.Success && SS3D.Core.SubSystems.TryGet(out TileSubSystem tiles) && tiles.IsServer)
                {
                    // Steel's breakup carries its break sound. Anything else has no message of its
                    // own, so it borrows the impact message with no surface and no particles.
                    if (wreckage.Kind != 0)
                        tiles.ServerAddSteelWreckage(wreckage);
                    else
                        tiles.ServerNotifyStructuralImpact(breakPoint, Vector3.zero, material, StructuralImpactCue.Break);
                }

                Log.Information(nameof(StructuralDamageService),
                    "Structural Destroyed at {coord} via {source} (force {force})",
                    Logs.Generic, coord, source, force);
                return true;
            }

            placed.ServerSetIntegrity(remaining, stage);

            if (placed.Layer == TileLayer.Turf && stage != previous && StructuralIntegrityRules.LeaksAtmosphere(stage))
            {
                Vector3 world = _query.TileToWorld(coord);
                // Atmos/vision refresh; Area still blocked by the wall until Destroyed.
                NotifyTileStateChanged(world);
            }

            return true;
        }

        private bool TryGetStructural(TileCoord coord, out PlacedTileObject placed)
        {
            return StructuralIntegrityRules.TryGetTarget(_query, coord, out placed);
        }

        private static void EnsureInitialized(PlacedTileObject placed, float max)
        {
            if (placed.IntegrityRemaining < 0f)
                placed.ServerSetIntegrity(max, StructuralIntegrityStage.Intact);
        }

        private void NotifyTileStateChanged(Vector3 world)
        {
            // Prefer TileSubSystem when registered (Play Mode); EditMode tests pass a map-backed notify via construction map.
            if (SS3D.Core.SubSystems.TryGet(out TileSubSystem tiles))
                tiles.NotifyTileStateChanged(world);
        }
    }
}
