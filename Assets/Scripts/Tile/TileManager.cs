using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
namespace TileMap {
    /**
     * Handles communication between tiles, and networking tile changes
     */
    [ExecuteAlways]
    public class TileManager : NetworkBehaviour
    {
        public IReadOnlyDictionary<ulong, TileObject> Tiles => tiles;
        public Vector3 Origin => origin;

        /**
         * Create a series of tiles at the given positions
         */
        public void InitializeTiles(Vector2 origin, List<Tuple<int, int, ConstructibleTile>> tileList)
        {
            tiles.Clear();
            this.origin = origin;

            foreach (var item in tileList) {
                ulong key = GetKey(item.Item1, item.Item2);
                tiles[key] = SpawnTileObject(item.Item1, item.Item2);
                tiles[key].Tile = item.Item3;
            }

            // Once they are all made go through and update all adjacencies.
            UpdateAllTileAdjacencies();
        }

        /**
         * Recreates tilemap using just children.
         */
        public void ReinitializeFromChildren()
        {
            tiles.Clear();
            LoadAllChildren();
            UpdateAllTileAdjacencies();
        }

        /**
         * Creates a tile at or near the given position
         */
        public void CreateTile(Vector3 position, ConstructibleTile tileInfo)
        {
            var index = GetIndexAt(position);
            CreateTile(index.x, index.y, tileInfo);
        }
        /**
         * Create a tile in the given grid position
         * 
         * Note: throws if a tile already exists
         */
        public void CreateTile(int x, int y, ConstructibleTile tileInfo) {
            ulong key = GetKey(x, y);

            if(tiles.ContainsKey(key)) {
                var message = "Tried to create tile that already exists at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]";
                Debug.LogError(message);
                throw new Exception(message);
            }

            var tileObject = SpawnTileObject(x, y);
            tiles[key] = tileObject;

            tileObject.Tile = tileInfo;
            var adjacents = GetAndUpdateAdjacentTiles(x, y, tileInfo);
            tileObject.UpdateAllAdjacencies(adjacents);
        }

        public void UpdateTile(Vector3 position, ConstructibleTile tileInfo)
        {
            var index = GetIndexAt(position);
            UpdateTile(index.x, index.y, tileInfo);
        }
        /**
         * Update a tile at the given index with the given information.
         * 
         * Note: Throws if there is no tile
         */
        public void UpdateTile(int x, int y, ConstructibleTile tileInfo)
        {
            ulong key = GetKey(x, y);

            if (!tiles.ContainsKey(key)) {
                throw new Exception("Tried to update tile that doesn't exist at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]");
            }

            var obj = tiles[key];
            obj.Tile = tileInfo;

            var adjacents = GetAndUpdateAdjacentTiles(x, y, tileInfo);
            obj.UpdateAllAdjacencies(adjacents);
        }

        /**
         * Destroy the given tile object
         */
        public void DestroyTile(TileObject tileObject) {
            var index = GetIndexAt(tileObject.transform.position);
            DestroyTile(index.x, index.y);
        }
        public void DestroyTile(int x, int y)
        {
            ulong key = GetKey(x, y);

            if(!tiles.ContainsKey(key)) {
                var message = "Tried to destroy tile that doesn't exist at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]";
                Debug.LogError(message);
                throw new Exception(message);
            }

            var obj = tiles[key];
            tiles.Remove(key);
            Destroy(obj);

            GetAndUpdateAdjacentTiles(x, y, ConstructibleTile.NullObject);
        }

        #if UNITY_EDITOR
        /**
         * Remove a tile from the map without destroying it.
         * Called when user deletes a tile in the editor
         */
        public void RemoveTile(TileObject tileObject)
        {
            var index = GetIndexAt(tileObject.transform.position);
            ulong key = GetKey(index.x, index.y);

            // If we initiated the tile destroying from the TileManager, then tiles[key] shouldn't exist, and we shouldn't repeat ourselves
            if(tiles.ContainsKey(key) && tiles[key] == tileObject) {
                GetAndUpdateAdjacentTiles(index.x, index.y, ConstructibleTile.NullObject);
                tiles.Remove(key);
            }
        }
        #endif

        public TileObject GetTile(Vector3 position)
        {
            var index = GetIndexAt(position);
            return GetTile(index.x, index.y);
        }

        /**
         * Returns null if no tile found
         */
        public TileObject GetTile(int x, int y)
        {
            TileObject tileObject;
            tiles.TryGetValue(GetKey(x, y), out tileObject);

            return tileObject;
        }

        public Vector2Int GetIndexAt(Vector3 position)
        {
            return new Vector2Int((int)Math.Round(position.x - origin.x), (int)Math.Round(position.z - origin.z));
        }
        public Vector3 GetPosition(int x, int y)
        {
            return origin + new Vector3(x * 1.0f, 0.0f, y * 1.0f);
        }

        // Perform the getting of children as early as possible in case we get queried.
        private void Start() => ReinitializeFromChildren();
        private void OnValidate()
        {
            if(tiles.Count > 0)
                return;

            // Can't do most things in OnValidate, so wait a sec.
            UnityEditor.EditorApplication.delayCall += ReinitializeFromChildren;
        }

        /**
         * Load all children into being tiles, but doesn't do anything with them.
         * Destroys any child which isn't a tile.
         */
        private void LoadAllChildren()
        {
            List<GameObject> queuedDestroy = new List<GameObject>();

            for (int i = 0; i < transform.childCount; i++) {
                var child = transform.GetChild(i);

                var childTile = child.GetComponent<TileObject>();
                if (childTile == null) {
                    Debug.LogWarning("TileMap has child which is not a tile: " + child.name);
                    continue;
                }

                // If the tile doesn't actually have anything, destroy it
                if (childTile.Tile.turf == null && childTile.Tile.fixture == null) {
                    queuedDestroy.Add(child.gameObject);
                    continue;
                }

                var index = GetIndexAt(childTile.transform.position);
                ulong key = GetKey(index.x, index.y);

                if(tiles.ContainsKey(key) && tiles[key] != childTile) {
                    Debug.LogWarning("Tile already exists at [" + index.x.ToString() + ", " + index.y.ToString() + "]. Deleting other.");
                    EditorAndRuntime.Destroy(child.gameObject);
                }
                else {
                    tiles[key] = childTile;
                }
            }

            foreach (var gameObject in queuedDestroy) {
                EditorAndRuntime.Destroy(gameObject);
            }
        }

        /**
         * Call UpdateSingleAdjacency on every tile adjacent to the provided one.
         * 
         * Returns adjacent tiles which may be used to update the central tile
         */
        private ConstructibleTile[] GetAndUpdateAdjacentTiles(int x, int y, ConstructibleTile tileInfo)
        {
            ConstructibleTile[] adjacents = new ConstructibleTile[8];

            // Then go for each adjacent and update
            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++) {
                // Take the cardinal direction, but use it in negative, so direction means the direction from OTHER to the just updated tile.
                var modifier = DirectionHelper.ToCardinalVector(direction);

                if (y + modifier.Item2 < 0 || x + modifier.Item1 < 0)
                    continue;

                ulong otherKey = GetKey(x + modifier.Item1, y + modifier.Item2);
                if(tiles.ContainsKey(otherKey)) {
                    adjacents[(int)direction] = tiles[otherKey].Tile;
                    tiles[otherKey].UpdateSingleAdjacency(DirectionHelper.GetOpposite(direction), tileInfo);
                }
            }

            return adjacents;
        }

        /**
         * Updates every tile in the tilemap
         */
        private void UpdateAllTileAdjacencies()
        {
            // Once they are all made go through and update all adjacencies.
            var adjacentTiles = new ConstructibleTile[8];
            foreach (var item in tiles) {
                int x = (int)(item.Key & 0xffff);
                int y = (int)(item.Key >> 32);

                // Find each adjacent tile and put into a map
                for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++) {
                    // Take the cardinal direction, but use it in negative, so direction means the direction from OTHER to the just updated tile.
                    var modifier = DirectionHelper.ToCardinalVector(direction);

                    if (y + modifier.Item2 < 0 || x + modifier.Item1 < 0)
                        adjacentTiles[(int)direction] = ConstructibleTile.NullObject;

                    ulong otherKey = GetKey(x + modifier.Item1, y + modifier.Item2);
                    if (tiles.ContainsKey(otherKey))
                        adjacentTiles[(int)direction] = tiles[otherKey].Tile;
                    else
                        adjacentTiles[(int)direction] = ConstructibleTile.NullObject;
                }

                item.Value.UpdateAllAdjacencies(adjacentTiles);
            }
        }

        private TileObject SpawnTileObject(int x, int y)
        {
            GameObject obj = new GameObject("[" + x.ToString() + "," + y.ToString() + "]", new [] { typeof(TileObject) });
            obj.transform.parent = transform;
            obj.transform.position = GetPosition(x, y);
            return obj.GetComponent<TileObject>();
        }
        private ulong GetKey(int x, int y)
        {
            return ((ulong)y << 32) + (ulong)x;
        }

        // TODO: This is an inefficient data structure for our purposes.
        // TODO: Allow negatives
        // The key is the concatenated y,x position of the tile.
        private Dictionary<ulong, TileObject> tiles = new Dictionary<ulong, TileObject>();

        private Vector3 origin = new Vector3(-100.0f, 0.0f, -100.0f);
    }
}