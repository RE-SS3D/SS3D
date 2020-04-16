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
    public class WallConstructer : MonoBehaviour, Interaction
    {
        [SerializeField]
        private Turf wallToConstruct = null;
        [SerializeField]
        private Turf floorToConstruct = null;

        public InteractionEvent Event { get; set; }
        public string Name => ShouldDeconstruct ? "Deconstruct Wall" : "Construct Wall";

        public bool CanInteract()
        {
            targetTile = Event.target.GetComponentInParent<TileObject>();

            return Event.tool == gameObject && targetTile != null;
        }

        public void Interact()
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