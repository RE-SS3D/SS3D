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
    public class WallConstructer : Core.InteractionComponent
    {
        [SerializeField]
        private Turf wallToConstruct;
        [SerializeField]
        private Turf floorToConstruct;

        public override bool CanInteract(GameObject tool, GameObject target, RaycastHit at)
        {
            targetTile = target.GetComponentInParent<TileObject>();

            return tool == gameObject && targetTile != null;
        }

        public override void Interact(GameObject tool, GameObject target, RaycastHit at)
        {
            // Note: CanInteract is always called before Interact, so we KNOW targetTile is defined.
            var tileManager = FindObjectOfType<TileManager>();

            var tile = targetTile.Tile;
            if (tile.turf?.isWall == true) // Deconstruct
                tile.turf = floorToConstruct;
            else // Construct
                tile.turf = wallToConstruct;

            tile.fixture = null;
            // TODO: Make an easier way of doing this.
            tile.subStates = new object[2];

            tileManager.UpdateTile(targetTile.transform.position, tile);
        }

        TileObject targetTile;
    }
}