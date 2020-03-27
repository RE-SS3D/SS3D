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
    public class TableConstructer : MonoBehaviour, Interaction
    {
        [SerializeField]
        private Fixture tableToConstruct = null;

        public InteractionEvent Event { get; set; }
        public string Name => ShouldDeconstruct ? "Deconstruct Table" : "Construct Table";

        public bool CanInteract()
        {
            targetTile = Event.target.GetComponentInParent<TileObject>();

            return Event.tool == gameObject && targetTile != null && targetTile.Tile.turf?.isWall == false;
        }

        public void Interact()
        {
            // Note: CanInteract is always called before Interact, so we KNOW targetTile is defined.
            var tileManager = FindObjectOfType<TileManager>();

            var tile = targetTile.Tile;

            if (tile.fixture == tableToConstruct) // Deconstruct
                tile.fixture = null;
            else // Construct
                tile.fixture = tableToConstruct;

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