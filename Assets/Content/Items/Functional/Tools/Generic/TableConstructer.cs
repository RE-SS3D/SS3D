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
    public class TableConstructer : DelayedInteraction
    {
        [SerializeField]
        private Fixture tableToConstruct = null;

        public override InteractionEvent Event { get; set; }
        public override string Name => ShouldDeconstruct ? "Deconstruct Table" : "Construct Table";

        // The distance in which to allow constructing tables.
        public float buildDistance = 1.5f;

        public override bool CanInteract()
        {
            if (!base.CanInteract())
            {
                return false;
            }
            
            targetTile = Event.target.GetComponentInParent<TileObject>();

            // Note: I didn't write the failure conditions over here, just rewrote in a different way.
            // Not quite sure what the second one does.

            // If target tile exists.
            if (targetTile == null)
            {
                return false;
            }

            // Dont construct if picking up the item.
            if (Event.tool != gameObject)
            {
                return false;
            }

            // Range check
            if (Vector3.Distance(Event.Player.transform.position, Event.target.transform.position) > 1.5f)
            {
                return false;
            }

            // Make sure there's not a wall on the turf.
            if (targetTile.Tile.turf?.isWall == true)
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

            if (tile.fixture != null) // If there is a fixture on the place
            {
                if (tile.fixture == tableToConstruct) // If the fixture is a table
                {
                    tile.fixture = null; // Deconstruct
                }
            }
            else // If there is no fixture on place
            {
                tile.fixture = tableToConstruct; // Construct
            }

            // TODO: Make an easier way of doing this.
            tile.subStates = new object[2];
            tile.subStates[0] = tile.subStates?[0] ?? null;
            tile.subStates[1] = null;

            tileManager.UpdateTile(targetTile.transform.position, tile);
        }

        bool ShouldDeconstruct => targetTile.Tile.fixture == tableToConstruct;
        TileObject targetTile;
    }
}