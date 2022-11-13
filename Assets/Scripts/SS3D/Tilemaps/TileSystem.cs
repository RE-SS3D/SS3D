using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Tilemaps
{
    public class TileSystem : NetworkedSystem
    {
        [SyncObject] private readonly SyncDictionary<Vector3Int, Tile> _tiles = new();

        public Tile TilePrefab;

        public Dictionary<Vector3Int, Tile> Tiles => _tiles.GetCollection(true);

        protected override void OnStart()
        {
            base.OnStart();

            _tiles.OnChange += HandleTilesChanged;
        }

        private void HandleTilesChanged(SyncDictionaryOperation op, Vector3Int position, Tile tile, bool asServer)
        {
            TileChangedEvent tileChangedEvent = new(tile);

            tileChangedEvent.Invoke(this);
        }

        public Tile GetTile(Vector3Int position)
        {
            if (_tiles.TryGetValue(position, out Tile tile))
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

        [Server]
        public bool TryPlaceTile(Vector3Int position, out Tile placedTile)
        {
            if (!IsPositionEmpty(position))
            {
                placedTile = null;
                return false;
            }

            Tile newTile = Instantiate(TilePrefab, TransformCache);
            ServerManager.Spawn(newTile.GameObjectCache);

            if (_tiles.TryGetValue(position, out Tile tile))
            {
                if (tile == null)
                {
                    _tiles.Add(position, newTile);   
                }
            }
            else
            {
                _tiles.Add(position, newTile);
            }

            newTile.Position = position;
            placedTile = newTile;
            return true;
        }

        private bool IsPositionEmpty(Vector3Int position)
        {
            _tiles.TryGetValue(position, out Tile tile);

            return tile == null;
        }

    }
}