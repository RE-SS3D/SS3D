using UnityEngine;
using SS3D.Engine.Tiles;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Items.Functional.Tools
{
    /**
     * Constructs and deconstructs tables
     * 
     * <inheritdoc cref="Core.Interaction" />
     */
    public class WallConstructer : DelayedInteraction
    {
        [SerializeField]
        private Turf wallToConstruct = null;
        [SerializeField]
        private Turf floorToConstruct = null;

        public override InteractionEvent Event { get; set; }
        public override string Name => ShouldDeconstruct ? "Deconstruct Wall" : "Construct Wall";

        // The distance in which to allow constructing walls
        public float buildDistance = 1.5f;

        public override bool CanInteract()
        {
            if (!base.CanInteract())
            {
                return false;
            }
            
            targetTile = Event.target.GetComponentInParent<TileObject>();

            // If target tile exists.
            if (targetTile == null)
            {
                return false;
            }

            //Dont construct if picking up the item.
            if (Event.tool != gameObject)
            {
                return false;
            }

            // Range check
            if (Vector3.Distance(Event.Player.transform.position, Event.target.transform.position) > 1.5f)
            {
                return false;
            }


            //The target tile's.... Tile.
            var tile = targetTile.Tile;

            // Prevent construction if the tile is occupied by a fixture. 
            if (tile.fixture != null) 
            {
                return false;
            }

            return true;
        }

        protected override void InteractDelayed()
        {
            // Note: CanInteract is always called before Interact, so we KNOW targetTile is defined.
            var tileManager = FindObjectOfType<TileManager>();


            var tile = targetTile.Tile;

            if (tile.fixture != null) // Prevent construction if the tile is occupied by a fixture. 
                return;
            if (tile.turf?.isWall == true) // Deconstruct
                tile.turf = floorToConstruct;
            else // Construct
                tile.turf = wallToConstruct;

            tile.fixture = null;
            // TODO: Make an easier way of doing this.
            tile.subStates = new object[2];

            tileManager.UpdateTile(targetTile.transform.position, tile);
        }

        bool ShouldDeconstruct => targetTile.Tile.turf?.isWall == true;
        TileObject targetTile;
    }
}