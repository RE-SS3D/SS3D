using UnityEngine;
using System.Collections;
using Mirror;
using TileMap;

namespace Interactions2.Custom
{
    /**
     * Constructs and deconstructs tables
     * 
     * <inheritdoc cref="Core.Interaction" />
     */
    public class TableConstructer : Core.InteractionComponent
    {
        [SerializeField]
        private Fixture tableToConstruct;

        public override bool CanInteract(GameObject tool, GameObject target, RaycastHit at)
        {
            targetTile = target.GetComponentInParent<TileObject>();

            return tool == gameObject && targetTile != null && targetTile.Tile.turf?.isWall == false;
        }

        public override void Interact(GameObject tool, GameObject target, RaycastHit at)
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

        TileObject targetTile;
    }
}