using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interaction.Core;
using TileMap;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class WallDeconstructable : MonoBehaviour, ISingularInteraction
    {
        [SerializeField]
        private InteractionKind wallDeconstructKind = null;
        [SerializeField]
        private Turf tileToConstruct = null;

        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(wallDeconstructKind);
        }

        public bool Handle(InteractionEvent e)
        {
            if (!tile.Tile.turf.isWall)
                return false;

            // Run in coroutine to prevent destroy-while-in-use errors
            StartCoroutine(DeconstructWall(e));

            return true;
        }

        private void Awake()
        {
            tile = transform.parent.GetComponent<TileObject>();
        }

        private IEnumerator DeconstructWall(InteractionEvent e)
        {
            // Construct a wall on this spot
            var newTileDefinition = tile.Tile;
            newTileDefinition.turf = tileToConstruct;
            newTileDefinition.fixture = null; // TODO: Assumes fixtures are incompatible between wall and tile for now
            // TODO: code dealing with all possibilities of substates should be moved into TileDefinition
            newTileDefinition.subStates = new object[2];

            var playerClient = e.player.GetComponent<PlayerTileManagerClient>();

            // Wait a tick to update the tile otherwise the interaction system complains
            yield return new WaitForEndOfFrame();
            playerClient.UpdateTile(tile, newTileDefinition);
        }

        private TileObject tile;
    }
}
