using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace TileMap {
    /**
     * Handles communication between tiles, and networking tile changes
     */
    [ExecuteAlways]
    public class TileManager : NetworkBehaviour
    {
        /**
         * Describes a tile in networkable information
         */
        private struct NetworkableTileDefinition
        {
            // The base of the tile, could be a wall or floor. Is id of Turf scriptable object
            public string turf;
            public string fixture; // Id of a Fixture scriptable object

            // Serialized objects
            public object[] subStates;
        }

        private struct NetworkableTileObject
        {
            public Vector2Int position;
            public NetworkableTileDefinition definition;
        }

        // Should put all turfs and fixtures that are usable in the map here.
        // TODO: Debate putting them in Resources and auto loading them on startup,
        // so they don't have to be provided
        public IReadOnlyDictionary<ulong, TileObject> Tiles => tiles;
        public Vector3 Origin => origin;

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
        public void CreateTile(Vector3 position, TileDefinition tileInfo)
        {
            var index = GetIndexAt(position);
            CreateTile(index.x, index.y, tileInfo);
        }
        /**
         * Create a tile in the given grid position
         * 
         * Note: throws if a tile already exists
         */
        public void CreateTile(int x, int y, TileDefinition tileInfo) {
#if UNITY_EDITOR
            if (Application.isPlaying)
                CmdCreateTile(x, y, ToNetworkable(tileInfo));
            else
                ExecuteCreateTile(x, y, tileInfo);
#else
            CmdCreateTile(x, y, ToNetworkable(tileInfo));
#endif
        }

        public void UpdateTile(Vector3 position, TileDefinition tileInfo)
        {
            var index = GetIndexAt(position);
            UpdateTile(index.x, index.y, tileInfo);
        }
        /**
         * Update a tile at the given index with the given information.
         * 
         * Note: Throws if there is no tile
         */
        public void UpdateTile(int x, int y, TileDefinition tileInfo)
        {
#if UNITY_EDITOR
            if(Application.isPlaying)
                CmdUpdateTile(x, y, ToNetworkable(tileInfo));
            else
                ExecuteUpdateTile(x, y, tileInfo);
#else
            CmdUpdateTile(x, y, ToNetworkable(tileInfo));
#endif
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
#if UNITY_EDITOR
            if (Application.isPlaying)
                CmdDestroyTile(x, y);
            else
                ExecuteDestroyTile(x, y);
#else
            CmdDestroyTile(x, y);
#endif
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
                GetAndUpdateAdjacentTiles(index.x, index.y, TileDefinition.NullObject);
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
        /**
         * Gets the closest tile position to the given position
         */
        public Vector3 GetPositionClosestTo(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x - Origin.x) + Origin.x,
                0.0f,
                Mathf.Round(position.z - Origin.z) + Origin.z
            );
        }

        public void Awake()
        {
            turfs = Resources.FindObjectsOfTypeAll<Turf>();
            fixtures = Resources.FindObjectsOfTypeAll<Fixture>();
        }
        public override void OnStartServer() => ReinitializeFromChildren();
        public override void OnStartLocalPlayer()
        {
            InitializeTilesFromServer();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(tiles.Count > 0)
                return;

            // Can't do most things in OnValidate, so wait a sec.
            UnityEditor.EditorApplication.delayCall += () => {
                if(this)
                    ReinitializeFromChildren();
            };
        }
#endif

        /**
         * Create a series of tiles at the given positions
         */
        private void InitializeTilesFromServer()
        {
            tiles.Clear();
            
            // TODO: Origins
            // this.origin = origin;
            var tileList = CmdGetTileMap();

            foreach (var item in tileList) {
                ulong key = GetKey(item.position.x, item.position.y);
                tiles[key] = SpawnTileObject(item.position.x, item.position.y);
                tiles[key].Tile = FromNetworkable(item.definition);
            }

            // Once they are all made go through and update all adjacencies.
            UpdateAllTileAdjacencies();
        }

        [Command]
        private List<NetworkableTileObject> CmdGetTileMap()
        {
            return tiles.Values.Select(tile => new NetworkableTileObject {
                position = GetIndexAt(tile.transform.position),
                definition = ToNetworkable(tile.Tile)
            }).ToList();
        }

        // If theres a more compact way of doing this, please tell me.
        [Command]
        private void CmdCreateTile(int x, int y, NetworkableTileDefinition definition)
        {
            ExecuteCreateTile(x, y, FromNetworkable(definition));
            RpcCreateTile(x, y, definition);
        }
        [ClientRpc]
        private void RpcCreateTile(int x, int y, NetworkableTileDefinition definition)
        {
            if(!isServer)
                ExecuteCreateTile(x, y, FromNetworkable(definition));
        }
        /**
         * Creates a tile from networked information.
         */
        private void ExecuteCreateTile(int x, int y, TileDefinition tileDefinition)
        {
            ulong key = GetKey(x, y);

            if (tiles.ContainsKey(key)) {
                var message = "Tried to create tile that already exists at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]";
                Debug.LogError(message);
                throw new Exception(message);
            }

            var tileObject = SpawnTileObject(x, y);
            tiles[key] = tileObject;

            tileObject.Tile = tileDefinition;
            var adjacents = GetAndUpdateAdjacentTiles(x, y, tileDefinition);
            tileObject.UpdateAllAdjacencies(adjacents);
        }
        
        [Command]
        private void CmdUpdateTile(int x, int y, NetworkableTileDefinition definition)
        {
            ExecuteUpdateTile(x, y, FromNetworkable(definition));
            RpcUpdateTile(x, y, definition);
        }
        [ClientRpc]
        private void RpcUpdateTile(int x, int y, NetworkableTileDefinition definition)
        {
            if (!isServer)
                ExecuteUpdateTile(x, y, FromNetworkable(definition));
        }
        private void ExecuteUpdateTile(int x, int y, TileDefinition tileDefinition)
        {
            ulong key = GetKey(x, y);

            if (!tiles.ContainsKey(key)) {
                throw new Exception("Tried to update tile that doesn't exist at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]");
            }

            var obj = tiles[key];
            obj.Tile = tileDefinition;

            var adjacents = GetAndUpdateAdjacentTiles(x, y, tileDefinition);
            obj.UpdateAllAdjacencies(adjacents);
        }

        [Command]
        private void CmdDestroyTile(int x, int y)
        {
            ExecuteDestroyTile(x, y);
            RpcDestroyTile(x, y);
        }
        [ClientRpc]
        private void RpcDestroyTile(int x, int y)
        {
            if (!isServer)
                ExecuteDestroyTile(x, y);
        }
        private void ExecuteDestroyTile(int x, int y)
        {
            ulong key = GetKey(x, y);

            if (!tiles.ContainsKey(key)) {
                var message = "Tried to destroy tile that doesn't exist at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]";
                Debug.LogError(message);
                throw new Exception(message);
            }

            var obj = tiles[key];
            tiles.Remove(key);
            Destroy(obj);

            GetAndUpdateAdjacentTiles(x, y, TileDefinition.NullObject);
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

                #if UNITY_EDITOR
                // It may be a ghost tile, which should be ignored.
                if(child.tag == "EditorOnly") {
                    continue;
                }
                #endif

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
        private TileDefinition[] GetAndUpdateAdjacentTiles(int x, int y, TileDefinition tileInfo)
        {
            TileDefinition[] adjacents = new TileDefinition[8];

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
            var adjacentTiles = new TileDefinition[8];
            foreach (var item in tiles) {
                int x = (int)(item.Key & 0xffff);
                int y = (int)(item.Key >> 32);

                // Find each adjacent tile and put into a map
                for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++) {
                    // Take the cardinal direction, but use it in negative, so direction means the direction from OTHER to the just updated tile.
                    var modifier = DirectionHelper.ToCardinalVector(direction);

                    if (y + modifier.Item2 < 0 || x + modifier.Item1 < 0)
                        adjacentTiles[(int)direction] = TileDefinition.NullObject;

                    ulong otherKey = GetKey(x + modifier.Item1, y + modifier.Item2);
                    if (tiles.ContainsKey(otherKey))
                        adjacentTiles[(int)direction] = tiles[otherKey].Tile;
                    else
                        adjacentTiles[(int)direction] = TileDefinition.NullObject;
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

        /**
         * Converts a tile definition into a networkable form
         */
        private NetworkableTileDefinition ToNetworkable(TileDefinition tileDefinition)
        {
            return new NetworkableTileDefinition {
                turf = tileDefinition.turf?.id,
                fixture = tileDefinition.fixture?.id,
                subStates = tileDefinition.subStates
            };
        }
        /**
         * Converts a networkable definition back to it's original form
         */
        private TileDefinition FromNetworkable(NetworkableTileDefinition tileDefinition)
        {
            var turfObject = turfs.FirstOrDefault(turf => turf.id == tileDefinition.turf);
            if(turfObject == null && !String.IsNullOrEmpty(tileDefinition.turf)) {
                Debug.LogError($"Failed to find turf with id {tileDefinition.turf} from network source");
            }
            var fixtureObject = fixtures.FirstOrDefault(fixture => fixture.id == tileDefinition.fixture);
            if(fixtureObject == null && !String.IsNullOrEmpty(tileDefinition.fixture)) {
                Debug.LogError($"Failed to find fixture with id {tileDefinition.fixture} from network source");
            }

            return new TileDefinition {
                turf = turfObject,
                fixture = fixtureObject,
                subStates = tileDefinition.subStates,
            };
        }

        // TODO: This is an inefficient data structure for our purposes.
        // TODO: Allow negatives
        // The key is the concatenated y,x position of the tile.
        private Dictionary<ulong, TileObject> tiles = new Dictionary<ulong, TileObject>();

        private Vector3 origin = new Vector3(-100.0f, 0.0f, -100.0f);

        // All turfs and fixtures that are usable
        private Turf[] turfs;
        private Fixture[] fixtures;
    }
}