﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interaction.Core;
using TileMap;

namespace Interaction
{
    public class WallDeconstructable : MonoBehaviour, ISingularInteraction
    {
        [SerializeField]
        private InteractionKind wallDeconstructKind;
        [SerializeField]
        private Turf tileToConstruct;

        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(wallDeconstructKind);
        }

        public bool Handle(InteractionEvent e)
        {
            if (!tile.Tile.turf.isWall)
                return false;

            // Run in coroutine to prevent destroy-while-in-use errors
            StartCoroutine(DeconstructWall());

            return true;
        }

        private void Awake()
        {
            tile = transform.parent.GetComponent<TileObject>();
        }

        private IEnumerator DeconstructWall()
        {
            // Construct a wall on this spot
            var newTileDefinition = tile.Tile;
            newTileDefinition.turf = tileToConstruct;
            newTileDefinition.fixture = null; // TODO: Assumes fixtures are incompatible between wall and tile for now
            // TODO: code dealing with all possibilities of substates should be moved into TileDefinition
            newTileDefinition.subStates = new object[2];

            var tileMap = tile.transform.parent.GetComponent<TileManager>();

            // Wait a tick to update the tile otherwise the interaction system complains
            yield return new WaitForEndOfFrame();
            tileMap.UpdateTile(transform.position, newTileDefinition);
        }

        private TileObject tile;
    }
}
