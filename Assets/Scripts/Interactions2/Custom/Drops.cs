using UnityEngine;
using System.Collections;
using Inventory.Custom;
using TileMap;

namespace Interactions2.Custom
{
    /**
     * <summary>
     * Drops the item in hand onto the target surface <br/>
     * This should be a universal interaction.
     * </summary>
     * <inheritdoc cref="Core.Interaction"/>
     */
    [CreateAssetMenu(fileName = "Drops", menuName = "Interactions2/Drop")]
    public class Drops : Core.InteractionSO
    {
        public override bool CanInteract(GameObject tool, GameObject target, RaycastHit at)
        {
            // An item can be dropped on a floor or any fixture attached to a floor.

            var tile = target.transform.parent?.GetComponent<TileObject>();
            var isFloor = (tile?.Tile.turf?.isWall ?? true) == false;

            return tool.transform.root.GetComponent<Hands>().GetItemInHand() != null && isFloor;
        }

        public override void Interact(GameObject tool, GameObject target, RaycastHit at)
        {
            tool.transform.root
                .GetComponent<Hands>()
                .PlaceHeldItem(
                    at.point + at.normal * 0.1f,
                    // Item is rotated to point in direction of interaction from player
                    Quaternion.LookRotation(tool.transform.root.up, at.point - tool.transform.root.position)
                );
        }
    }
}
