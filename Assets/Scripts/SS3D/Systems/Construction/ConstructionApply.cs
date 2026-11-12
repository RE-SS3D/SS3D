using SS3D.Core;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Server side of a finished construction step: apply the outcome, then spend the materials. Nothing
    /// is spent when the outcome fails (construction.md §3: a cancelled step costs time, never sheets).
    /// </summary>
    public static class ConstructionApply
    {
        /// <summary>Completes a cell action at <paramref name="grid"/>.</summary>
        public static bool TryApplyCell(ConstructionRule rule, Vector3 grid, TileObjectSo result, Item item, Hand hand)
        {
            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.Construction == null)
            {
                return false;
            }

            switch (rule.Outcome)
            {
                case ConstructionOutcome.Place:
                {
                    if (result == null || tiles.CurrentMap == null)
                    {
                        return false;
                    }

                    bool replace = ConstructionCellRules.ReplacesExisting(tiles.CurrentMap, rule, grid);
                    using (tiles.KeepWreckage())
                    {
                        if (!tiles.Construction.TryPlaceTile(result, grid, Direction.North, replace).Success)
                        {
                            return false;
                        }
                    }

                    break;
                }
                case ConstructionOutcome.ClearWreckage:
                {
                    if (tiles.CurrentMap == null
                        || !tiles.RemoveWreckage(ConstructionCellRules.CoordOf(tiles.CurrentMap, grid), TileLayer.Turf))
                    {
                        return false;
                    }

                    break;
                }
                default:
                    return false;
            }

            Spend(rule, item);
            GiveReturns(rule, hand, grid);
            return true;
        }

        /// <summary>Completes an object step on <paramref name="target"/>. Re-checks the target first.</summary>
        public static bool TryApplyObject(ConstructionRule rule, PlacedTileObject target, Item item, Hand hand)
        {
            if (target == null || !ConstructionObjectRules.Matches(rule, target)
                || !SubSystems.TryGet(out TileSubSystem tiles) || tiles.Construction == null)
            {
                return false;
            }

            Vector3 world = GridWorld(target);

            // "Construct girder" on a floor tile, "Clear wreckage" on plating: the outcome lands on the cell.
            if (rule.ActsOnCell)
            {
                string resultName = ResultNameFor(rule, item, tiles);
                TileObjectSo result = string.IsNullOrEmpty(resultName) ? null : tiles.GetAsset(resultName) as TileObjectSo;
                return TryApplyCell(rule, world, result, item, hand);
            }

            bool cracked = target.IntegrityStage == StructuralIntegrityStage.Cracked;
            string returnName = ReturnNameFor(rule, target);

            switch (rule.Outcome)
            {
                case ConstructionOutcome.Repair:
                    Repair(target, tiles.NotifyTileStateChanged);
                    break;
                case ConstructionOutcome.Remove:
                {
                    if (!tiles.Construction.TryClearTile(world, rule.Layer, target.Direction).Success)
                    {
                        return false;
                    }

                    break;
                }
                case ConstructionOutcome.Replace:
                {
                    // A stage step swaps the tile (D1): identity syncs, saves and reaches late joiners.
                    if (tiles.GetAsset(rule.ResultName) is not TileObjectSo next)
                    {
                        return false;
                    }

                    using (tiles.KeepWreckage())
                    {
                        if (!tiles.Construction.TryPlaceTile(next, world, target.Direction, replaceExisting: true).Success)
                        {
                            return false;
                        }
                    }

                    break;
                }
                default:
                    return false;
            }

            Spend(rule, item);
            if (!(cracked && rule.NoReturnWhenCracked))
            {
                GiveReturns(rule, hand, world, returnName);
            }

            return true;
        }

        /// <summary>
        /// Weld repair (structural plan Phase 6): straight back to Intact at full integrity. The presenter
        /// clears the stage overlays from <see cref="PlacedTileObject.ServerSetIntegrity"/>; a Cracked wall
        /// leaked, so <paramref name="notifyStateChanged"/> lets atmos and occupancy see it sealed again.
        /// </summary>
        public static void Repair(PlacedTileObject target, System.Action<Vector3> notifyStateChanged)
        {
            StructuralIntegrityStage previous = target.IntegrityStage;
            target.ServerSetIntegrity(StructuralIntegrityRules.ResolveMaxIntegrity(target.tileObjectSO),
                StructuralIntegrityStage.Intact);

            if (target.Layer == TileLayer.Turf && StructuralIntegrityRules.LeaksAtmosphere(previous))
            {
                notifyStateChanged?.Invoke(GridWorld(target));
            }
        }

        /// <summary>
        /// Hands back what a step returns (a pried tile, pulled sheets): into a free or matching hand,
        /// else onto the floor at <paramref name="world"/>. Salvage sets its count explicitly (D12).
        /// </summary>
        /// <param name="itemName">Item to hand back in place of the rule's own (the pried floor's kind of tile).</param>
        public static void GiveReturns(ConstructionRule rule, Hand hand, Vector3 world, string itemName = null)
        {
            itemName ??= rule.ReturnItemName;
            if (string.IsNullOrEmpty(itemName) || rule.ReturnCount <= 0)
            {
                return;
            }

            if (!SubSystems.TryGet(out TileSubSystem tiles) || !SubSystems.TryGet(out ItemSubSystem items)
                || tiles.GetAsset(itemName) is not ItemObjectSo itemSo || itemSo.PrefabAsset == null)
            {
                return;
            }

            Vector3 drop = world + Vector3.up * 0.2f;
            Item spawned = items.SpawnItemAtRest(itemSo.PrefabAsset.Id, drop);
            if (spawned == null)
            {
                return;
            }

            spawned.SetStackCount(rule.ReturnCount);

            Hands hands = hand != null ? hand.GetComponentInParent<Hands>() : null;
            if (hands != null)
            {
                foreach (Hand candidate in hands.PlayerHands)
                {
                    if (candidate != null && candidate.Container != null
                        && candidate.Container.CanContainItem(spawned) && candidate.Container.AddItem(spawned))
                    {
                        return;
                    }
                }
            }

            ItemUtility.Place(spawned, drop, spawned.transform.rotation);
            // Pass the lifted pose from ItemUtility.Place (inventory Pitfall: the raw point sinks the item).
            tiles.Construction.TryPlaceItem(itemSo, spawned.transform.position, spawned.transform.rotation, spawned.gameObject);
        }

        /// <summary>The tile a step places: the floor matching the held tile item, else the rule's result.</summary>
        public static string ResultNameFor(ConstructionRule rule, Item item, TileSubSystem tiles)
        {
            if (rule.Step == ConstructionStep.PlaceFloorTile && item != null && item.Asset != null && tiles != null)
            {
                return ConstructionFloorTiles.TurfFor(tiles.GetAsset(item.Asset)?.NameString);
            }

            return rule.ResultName;
        }

        /// <summary>What a step hands back: the tile matching the pried floor (none for dirt or carpet), else the rule's item.</summary>
        public static string ReturnNameFor(ConstructionRule rule, PlacedTileObject target)
        {
            if (rule.Step == ConstructionStep.PryFloorTile)
            {
                return target != null ? ConstructionFloorTiles.ItemFor(target.NameString) ?? string.Empty : string.Empty;
            }

            return rule.ReturnItemName;
        }

        public static Vector3 GridWorld(PlacedTileObject placed) => new(placed.WorldOrigin.x, 0f, placed.WorldOrigin.y);

        /// <summary>Takes the rule's cost from the held stack. Tools are never spent.</summary>
        public static void Spend(ConstructionRule rule, Item item)
        {
            if (item != null && ConstructionInputs.IsMaterial(rule.Input) && rule.Cost > 0)
            {
                item.TryConsume(rule.Cost);
            }
        }
    }
}
