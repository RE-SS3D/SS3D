using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interaction.Core;
using TileMap;

namespace Interaction
{
    [CreateAssetMenu(menuName = "Interaction/Construction/WallConstructableOn")]
    public class WallConstructableOn : SingularInteraction
    {
        [SerializeField]
        private InteractionKind wallConstructKind;

        [SerializeField]
        private Turf wallToConstruct;

        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(wallConstructKind);
        }

        public override bool Handle(InteractionEvent e)
        {
            var tile = Receiver.transform.parent?.GetComponent<TileObject>();

            if (tile != null && tile.Tile.turf.isWall)
                return false;

            // Construct a table on this spot
            var newTileDefinition = tile.Tile;
            newTileDefinition.turf = wallToConstruct;
            newTileDefinition.fixture = null; // TODO: Assumes fixtures are incompatible between wall and tile for now
            newTileDefinition.subStates = new object[2];

            var playerClient = e.player.GetComponent<PlayerTileManagerClient>();
            playerClient.UpdateTile(tile, newTileDefinition);

            return true;
        }
    }
}
