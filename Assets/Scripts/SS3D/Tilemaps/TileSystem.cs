using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Tilemaps.Events;
using UnityEngine;

namespace SS3D.Tilemaps
{
    /// <summary>
    /// The tile system creates an abstract tile in empty spaces.
    /// </summary>
    public class TileSystem : NetworkedSystem
    {
        /// <summary>
        /// All the created tiles in the system.
        /// </summary>
        public Dictionary<Vector3Int, TileData?> Tiles { get; } = new();

        /// <summary>
        /// Gets a tile at a position, if no tiles is found a new one is created.
        /// </summary>
        /// <param name="position">The position for that tile.</param>
        /// <returns>The tile at that position.</returns>
        [Server]
        public TileData GetTile(Vector3Int position)
        {
            TileData? nullableTile = GetTileNullable(position);

            if (nullableTile == null)
            {
                Punpun.Panic(this, $"Couldn't find tile at {position.ToString()}", Logs.ServerOnly);
                throw new Exception("Something went extremely wrong within TileSystem.GetTile().");
            }

            TileData tile = (TileData) nullableTile;
            return tile;
        }

        /// <summary>
        /// Gets a tile at a position, the difference from GetTile is that this makes TileData nullable, so we can have null tiles with no problem.
        /// This avoids us having a tile at a certain key with empty data for no reason.
        /// </summary>
        /// <param name="position">The position for that tile.</param>
        /// <returns>The tile at that position.</returns>
        [Server]
        private TileData? GetTileNullable(Vector3Int position)
        {
            if (Tiles.TryGetValue(position, out TileData? tile))
            {
                return tile;
            }

            if (TryPlaceTile(position, out tile))
            {
                return tile;
            }

            Punpun.Panic(this, $"Couldn't create or get tile at {position.ToString()}", Logs.ServerOnly);
            return null;
        }

        /// <summary>
        /// Tries to place a tile at a position.
        /// </summary>
        /// <param name="position">The position for that tile.</param>
        /// <param name="placedTile">The tile at that position.</param>
        /// <returns></returns>
        [Server]
        private bool TryPlaceTile(Vector3Int position, out TileData? placedTile)
        {
            if (!IsPositionEmpty(position))
            {
                placedTile = null;
                return false;
            }

            placedTile = new TileData(position);
            Tiles.Add(position, placedTile);

            return true;
        }

        /// <summary>
        /// Checks if a position is empty
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        [Server]
        private bool IsPositionEmpty(Vector3Int position)
        {
            Tiles.TryGetValue(position, out TileData? tile);

            return tile == null;
        }

        [Server]
        private void HandleTilesChanged(SyncDictionaryOperation op, Vector3Int position, TileData? tile, bool asServer)
        {
            TileChangedEvent tileChangedEvent = new(tile);
            tileChangedEvent.Invoke(this);
        }
    }
}