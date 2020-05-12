using UnityEngine;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Tiles;
using SS3D.Engine.Interactions.Extensions;

namespace SS3D.Content.Systems.Interactions
{
    /**
     * <summary>
     * Drops the item in hand onto the target surface <br/>
     * This should be a universal interaction.
     * </summary>
     * <inheritdoc cref="Core.Interaction"/>
     */
    [CreateAssetMenu(fileName = "Drops", menuName = "Interactions2/Drop")]
    public class Drops : InteractionSO
    {
        public override string Name => "Drop";

        public float dropDistance = 1.5f;

        public override bool CanInteract()
        {
            // An item can be dropped on a floor or any fixture attached to a floor.

            var tile = Event.target.transform.parent?.GetComponent<TileObject>();
            var isFloor = (tile?.Tile.turf?.isWall ?? true) == false;

            hands = Event.Player.GetComponent<Hands>();

            if (hands.GetItemInHand() == null)
            {
                return false;
            }

            if (!isFloor)
            {
                return false;
            }

            // Range check
            if (Vector3.Distance(Event.Player.transform.position, Event.target.transform.position) > dropDistance)
            {
                return false;
            }

            return true;
        }

        public override void Interact()
        {
            hands.PlaceHeldItem(
                Event.at.point + Event.at.normal * 0.1f
            );
        }

        private Hands hands;
    }
}
