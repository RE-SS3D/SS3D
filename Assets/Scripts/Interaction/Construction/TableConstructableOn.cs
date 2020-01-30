using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interaction.Core;
using TileMap;

namespace Interaction.Construction
{
    [CreateAssetMenu(menuName = "Interaction/Construction/TableConstructableOn")]
    public class TableConstructableOn : SingularInteraction
    {
        [SerializeField]
        private InteractionKind tableConstructKind;
        [SerializeField]
        private Fixture tableToConstruct;

        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(tableConstructKind);
        }

        public override bool Handle(InteractionEvent e)
        {
            var tile = Receiver.transform.parent?.GetComponent<TileObject>();

            if (tile.Tile.turf == null || tile.Tile.turf.isWall)
                return false;

            var playerClient = e.player.GetComponent<PlayerTileManagerClient>();

            // Construct a table on this spot
            var newTileDefinition = tile.Tile;
            newTileDefinition.fixture = tableToConstruct;
            newTileDefinition.subStates = new object[2];

            playerClient.UpdateTile(tile, newTileDefinition);

            return true;
        }
    }
}
