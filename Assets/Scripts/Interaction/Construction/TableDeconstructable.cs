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

            StartCoroutine(DeconstructTable());

            return true;
        }

        private void Awake()
        {
            tile = transform.parent.GetComponent<TileObject>();
        }

        private IEnumerator DeconstructTable()
        {
            // Construct a table on this spot
            var newTileDefinition = tile.Tile;
            newTileDefinition.fixture = null;
            // TODO: code dealing with all possibilities of substates should be moved into TileDefinition
            if (newTileDefinition.subStates != null && newTileDefinition.subStates.Length >= 2)
                newTileDefinition.subStates[1] = null;

            var tileMap = tile.transform.parent.GetComponent<TileManager>();

            // Wait a tick to update the tile otherwise the interaction system complains
            yield return new WaitForEndOfFrame();
            tileMap.UpdateTile(transform.position, newTileDefinition);
        }

        private TileObject tile;
    }
}
