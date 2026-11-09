using SS3D.Core;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
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
