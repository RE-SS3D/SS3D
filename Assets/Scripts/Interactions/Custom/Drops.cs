using UnityEngine;
using System.Collections;
using Inventory.Custom;
using TileMap;

namespace Interactions.Custom
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
        public override string Name => "Drop";

        public override bool CanInteract()
        {
            // An item can be dropped on a floor or any fixture attached to a floor.

            var tile = Event.target.transform.parent?.GetComponent<TileObject>();
            var isFloor = (tile?.Tile.turf?.isWall ?? true) == false;

            hands = Event.Player.GetComponent<Hands>();

            return hands.GetItemInHand() != null && isFloor;
        }

        public override void Interact()
        {
            hands.PlaceHeldItem(
                Event.at.point + Event.at.normal * 0.1f,
                // Item is rotated to point in direction of interaction from player
                Quaternion.LookRotation(Event.Player.transform.up, Event.at.point - Event.Player.transform.position)
            );
        }

        private Hands hands;
    }
}
