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
                    if (result == null)
                    {
                        return false;
                    }

                    PlaceResult placed = tiles.Construction.TryPlaceTile(result, grid, Direction.North, replaceExisting: false);
                    if (!placed.Success)
                    {
                        return false;
                    }

                    break;
                }
                default:
                    return false;
            }

            Spend(rule, item);
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
                default:
                    return false;
            }

            Spend(rule, item);
            GiveReturns(rule, hand, world);
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
        public static void GiveReturns(ConstructionRule rule, Hand hand, Vector3 world)
        {
            if (string.IsNullOrEmpty(rule.ReturnItemName) || rule.ReturnCount <= 0)
            {
                return;
            }

            if (!SubSystems.TryGet(out TileSubSystem tiles) || !SubSystems.TryGet(out ItemSubSystem items)
                || tiles.GetAsset(rule.ReturnItemName) is not ItemObjectSo itemSo || itemSo.PrefabAsset == null)
            {
                return;
            }

            Vector3 drop = world + Vector3.up * 0.2f;
            Item spawned = items.SpawnItem(itemSo.PrefabAsset.Id, drop, Quaternion.identity);
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
