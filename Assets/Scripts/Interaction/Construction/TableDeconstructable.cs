using System;
using System.Collections;
using UnityEngine;
using Interaction.Core;
using TileMap;

namespace Interaction.Construction
{
    public class TableDeconstructable : MonoBehaviour, ISingularInteraction
    {
        [SerializeField]
        private InteractionKind tableDeconstructKind;

        [SerializeField]
        private Fixture tableToRemove;

        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(tableDeconstructKind);
        }

        public bool Handle(InteractionEvent e)
        {
            if (tile.Tile.fixture?.id != tableToRemove.id)
                return false;

            StartCoroutine(DeconstructTable(e));

            return true;
        }

        private void Awake()
        {
            tile = transform.parent.GetComponent<TileObject>();
        }

        private IEnumerator DeconstructTable(InteractionEvent e)
        {
            // Construct a table on this spot
            var newTileDefinition = tile.Tile;
            newTileDefinition.fixture = null;
            // TODO: code dealing with all possibilities of substates should be moved into TileDefinition
            if (newTileDefinition.subStates != null && newTileDefinition.subStates.Length >= 2)
                newTileDefinition.subStates[1] = null;

            var playerClient = e.player.GetComponent<PlayerTileManagerClient>();

            // Wait a tick to update the tile otherwise the interaction system complains
            yield return new WaitForEndOfFrame();
            playerClient.UpdateTile(tile, newTileDefinition);
        }

        private TileObject tile;
    }
}
