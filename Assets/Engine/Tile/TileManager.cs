using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using Mirror;
using UnityEditor;

namespace SS3D.Engine.Tiles {
    /**
     * Handles communication between tiles, and networking tile changes
     */
    [ExecuteAlways]
    public class TileManager : NetworkBehaviour
    {
        public static TileManager singleton { get; private set; }
        /// <summary>
        /// How many tiles are serialized per rpc
        /// </summary>
        private const int TilesSentPerCall = 50;
        
        private struct NetworkableTileObject
        {
            public Vector2Int position;
            public TileDefinition definition;
        }
        
        public static event System.Action tileManagerLoaded;

        public bool IsEnabled()
        {
            return gameObject.activeSelf;
        }
        public static bool IsOnServer(GameObject tileChild)
        {
            return tileChild.transform.root.GetComponent<NetworkIdentity>().isServer;
        }
        public static bool IsOnClient(GameObject tileChild)
        {
            return tileChild.transform.root.GetComponent<NetworkIdentity>().isClient;
        }

        public Vector3 Origin => origin;
        public int Count => tiles.Count;

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
        [Server]
        public void CreateTile(Vector3 position, TileDefinition definition)
        {
            var index = GetIndexAt(position);
            CreateTile(index.x, index.y, definition);
        }
        /**
         * Create a tile in the given grid position
         * 
         * Note: throws if a tile already exists
         */
        [Server]
        public void CreateTile(int x, int y, TileDefinition definition) => InternalCreateTile(x, y, definition);

        [Server]
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
        [Server]
        public void UpdateTile(int x, int y, TileDefinition definition) => InternalUpdateTile(x, y, definition);

        /**
         * Destroy the given tile object
         */
        [Server]
        public void DestroyTile(TileObject tileObject) {
            var index = GetIndexAt(tileObject.transform.position);
            DestroyTile(index.x, index.y);
        }
        [Server]
        public void DestroyTile(int x, int y) => InternalDestroyTile(x, y);

#if UNITY_EDITOR
        public void EditorCreateTile(int x, int y, TileDefinition definition) => InternalCreateTile(x, y, definition);
        public void EditorUpdateTile(int x, int y, TileDefinition definition) => InternalUpdateTile(x, y, definition);
        public void EditorDestroyTile(int x, int y) => InternalDestroyTile(x, y);

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

        /**
         * Returns null if no tile found
         */
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

        [Server]
        public void SendTilesToClient(NetworkConnection connection)
        {
            NetworkableTileObject[] tileObjects = tiles.Values.Select(tile => new NetworkableTileObject {
                position = GetIndexAt(tile.transform.position),
                definition = tile.Tile
            }).ToArray();
            
            NetworkableTileObject[][] chunks = new NetworkableTileObject[(tileObjects.Length + TilesSentPerCall - 1) / TilesSentPerCall][];
            for (var i = 0; i < chunks.Length; i++)
            {
                int chunkLength = i == chunks.Length - 1 ? tileObjects.Length % TilesSentPerCall : TilesSentPerCall;
                NetworkableTileObject[] chunk = new NetworkableTileObject[chunkLength];
                Array.Copy(tileObjects, i * TilesSentPerCall, chunk, 0, 
                    chunkLength);
                chunks[i] = chunk;
            }
            
            TargetStartTileStream(connection, origin);
            foreach (NetworkableTileObject[] chunk in chunks)
            {
                TargetReceiveChunkFromServer(
                    connection,
                    chunk
                );
            }
            TargetTilemapEnd(connection);
        }

        public override void OnStartServer() => ReinitializeFromChildren();

        private void OnEnable()
        {
            LoadTileMap();
        }

        private void Awake()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                singleton = this;
            }
        }


        private void LoadTileMap()
        {
            tileManagerLoaded?.Invoke();
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

        /// <summary>
        /// Signals to the client that the server will send the tilemap
        /// </summary>
        /// <param name="origin">The origin point of the tilemap</param>
        [TargetRpc]
        private void TargetStartTileStream(NetworkConnection connection, Vector3 origin)
        {
            DestroyChildren();

            this.origin = origin;
        }

        /// <summary>
        /// Called when the server has sent all tilemap chunks
        /// </summary>
        [TargetRpc]
        private void TargetTilemapEnd(NetworkConnection connection)
        {
            UpdateAllTileAdjacencies();
        }

        /// <summary>
        /// Sends a chunk to a client
        /// </summary>
        /// <param name="tileList">The tiles in this chunk</param>
        [TargetRpc]
        private void TargetReceiveChunkFromServer(NetworkConnection connection, NetworkableTileObject[] tileList)
        {
            foreach (var item in tileList) {
                try
                {
                    ulong key = GetKey(item.position.x, item.position.y);
                    tiles[key] = SpawnTileObject(item.position.x, item.position.y);
                    tiles[key].Tile = item.definition;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /*
         * These RPCs cause the given operations to be executed on all clients.
         * It will refuse to run on a client that is also the server, as that would cause an endless loop,
         * and it is already executed on the server.
         */

        [ClientRpc]
        private void RpcCreateTile(int x, int y, TileDefinition definition)
        {
            if(!isServer)
                InternalCreateTile(x, y, definition);
        }
        [ClientRpc]
        private void RpcUpdateTile(int x, int y, TileDefinition definition)
        {
            if (!isServer)
                InternalUpdateTile(x, y, definition);
        }
        [ClientRpc]
        private void RpcDestroyTile(int x, int y)
        {
            if (!isServer)
                InternalDestroyTile(x, y);
        }

        /*
         * The internals contain the actual logic of the above functions. They aren't put in the
         * public method as the public method needs to have warnings when not called from server
         */
        private void InternalCreateTile(int x, int y, TileDefinition definition)
        {
            ulong key = GetKey(x, y);

            if (tiles.ContainsKey(key)) {
                var message = "Tried to create tile that already exists at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]";
                Debug.LogError(message);
                throw new Exception(message);
            }

            var tileObject = SpawnTileObject(x, y);
            tiles[key] = tileObject;

            tileObject.Tile = definition;
            var adjacents = GetAndUpdateAdjacentTiles(x, y, definition);
            tileObject.UpdateAllAdjacencies(adjacents);

            // Run on every client.
            if (Application.isPlaying && isServer)
                RpcCreateTile(x, y, definition);
        }
        private void InternalUpdateTile(int x, int y, TileDefinition definition)
        {
            ulong key = GetKey(x, y);

            if (!tiles.ContainsKey(key)) {
                throw new Exception("Tried to update tile that doesn't exist at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]");
            }

            var obj = tiles[key];
            obj.Tile = definition;

            var adjacents = GetAndUpdateAdjacentTiles(x, y, definition);
            obj.UpdateAllAdjacencies(adjacents);

            if (Application.isPlaying && isServer)
                RpcUpdateTile(x, y, definition);
        }
        private void InternalDestroyTile(int x, int y)
        {
            ulong key = GetKey(x, y);

            if (!tiles.ContainsKey(key)) {
                var message = "Tried to destroy tile that doesn't exist at position" + GetPosition(x, y).ToString() + ", index [" + x.ToString() + ", " + y.ToString() + "]";
                Debug.LogError(message);
                throw new Exception(message);
            }

            var obj = tiles[key];
            tiles.Remove(key);
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                DestroyImmediate(obj.gameObject);
#else
            Destroy(obj);
#endif


            GetAndUpdateAdjacentTiles(x, y, TileDefinition.NullObject);

            if (Application.isPlaying && isServer)
                RpcDestroyTile(x, y);
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
                if (childTile.Tile.plenum == null && childTile.Tile.turf == null && childTile.Tile.fixtures == null) {
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
         * Just like my dad used to do
         */
        private void DestroyChildren()
        {
            tiles.Clear();
            for(int i = transform.childCount - 1; i >= 0; --i) {
                Destroy(transform.GetChild(i).gameObject);
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

        public List<TileObject> GetAllTiles()
        {
            return tiles.Values.ToList<TileObject>();
        }

        public TileObject[] GetAdjacentTileObjects(TileObject tileObject)
        {
            TileObject[] adjacents = new TileObject[8];
            var index = GetIndexAt(tileObject.transform.position);

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction += 1)
            {
                // Take the cardinal direction, but use it in negative, so direction means the direction from OTHER to the just updated tile.
                var modifier = DirectionHelper.ToCardinalVector(direction);

                if (index.y + modifier.Item2 < 0 || index.x + modifier.Item1 < 0)
                    continue;

                ulong otherKey = GetKey(index.x + modifier.Item1, index.y + modifier.Item2);
                if (tiles.ContainsKey(otherKey))
                {
                    adjacents[(int)direction] = tiles[otherKey];
                }
                else
                {
                    adjacents[(int)direction] = null;
                }
            }

            return adjacents;
        }

        // TODO: This is an inefficient data structure for our purposes.
        // TODO: Allow negatives
        // The key is the concatenated y,x position of the tile.
        private Dictionary<ulong, TileObject> tiles = new Dictionary<ulong, TileObject>();

        private Vector3 origin = new Vector3(-100.0f, 0.0f, -100.0f);
    }

    /**
     * Static methods for networking of tilemap
     * Note: Networkability of tiles relies on tiles being located in the Resources folder.
     */
    public static class TileNetworkingProperties
    {
        // TODO: This could be made more efficient. Especially when the subStates are all nulls.

        public static void WriteNetworkableTileDefinition(this NetworkWriter writer, TileDefinition definition)
        {
            // Write plenum
            writer.WriteString(definition.plenum?.name ?? "");

            // Write turf
            writer.WriteString(definition.turf?.name ?? "");

            // Write all tile fixtures
            foreach (TileFixtureLayers layer in TileDefinition.GetTileFixtureLayerNames())
            {
                Fixture f = definition.fixtures.GetTileFixtureAtLayer(layer);
                if (f)
                {
                    writer.WriteString(f.name ?? "");
                }
                else
                {
                    writer.WriteString("");
                }

                
            }

            // Write all wall fixtures
            foreach (WallFixtureLayers layer in TileDefinition.GetWallFixtureLayerNames())
            {
                Fixture f = definition.fixtures.GetWallFixtureAtLayer(layer);
                if (f)
                {
                    writer.WriteString(f.name ?? "");
                }
                else
                {
                    writer.WriteString("");
                }
            }

            // Write all floor fixtures
            foreach (FloorFixtureLayers layer in TileDefinition.GetFloorFixtureLayerNames())
            {
                Fixture f = definition.fixtures.GetFloorFixtureAtLayer(layer);
                if (f)
                {
                    writer.WriteString(f.name ?? "");
                }
                else
                {
                    writer.WriteString("");
                }
            }

            // Use C# serializer to serialize the object array, cos the Mirror one isn't powerful enough.

            // Can't serialize null values so put a boolean indicating array presence first
            if (definition.subStates == null || definition.subStates.All(obj => obj == null)) {
                writer.WriteBoolean(false);
            }
            else {
                writer.WriteBoolean(true);

                using(var stream = new MemoryStream()) {
                    new BinaryFormatter().Serialize(stream, definition.subStates);
                    writer.WriteBytesAndSize(stream.ToArray());
                }
            }
        }
        public static TileDefinition ReadNetworkableTileDefinition(this NetworkReader reader)
        {
            TileDefinition tileDefinition = new TileDefinition();
            tileDefinition.fixtures = new FixturesContainer();

            // Read plenum
            string plenumName = reader.ReadString();
            if (!string.IsNullOrEmpty(plenumName))
            {
                tileDefinition.plenum = plenums.FirstOrDefault(plenum => plenum.name == plenumName);
                if (tileDefinition.plenum == null)
                    Debug.LogError($"Network recieved plenum with name {plenumName} could not be found");
            }

            // Read turf
            string turfName = reader.ReadString();
            if (!string.IsNullOrEmpty(turfName))
            {
                tileDefinition.turf = turfs.FirstOrDefault(turf => turf.name == turfName);
                if (tileDefinition.turf == null)
                    Debug.LogError($"Network recieved turf with name {turfName} could not be found");
            }

            // Read tile fixtures
            foreach (TileFixtureLayers layer in TileDefinition.GetTileFixtureLayerNames())
            {
                string fixtureName = reader.ReadString();
                if (!string.IsNullOrEmpty(fixtureName))
                {
                    TileFixture tf = (TileFixture)fixtures.FirstOrDefault(fixture => fixture.name == fixtureName);

                    tileDefinition.fixtures.SetTileFixtureAtLayer(tf, layer);
                    if (tf == null)
                    {
                        Debug.LogError($"Network recieved fixture with name {fixtureName} could not be found");
                    }
                }
            }

            // Read wall fixtures
            foreach (WallFixtureLayers layer in TileDefinition.GetWallFixtureLayerNames())
            {
                string fixtureName = reader.ReadString();
                if (!string.IsNullOrEmpty(fixtureName))
                {
                    WallFixture wf = (WallFixture)fixtures.FirstOrDefault(fixture => fixture.name == fixtureName);

                    tileDefinition.fixtures.SetWallFixtureAtLayer(wf, layer);
                    if (wf == null)
                    {
                        Debug.LogError($"Network recieved fixture with name {fixtureName} could not be found");
                    }
                }
            }

            // Read floor fixtures
            foreach (FloorFixtureLayers layer in TileDefinition.GetFloorFixtureLayerNames())
            {
                string fixtureName = reader.ReadString();
                if (!string.IsNullOrEmpty(fixtureName))
                {
                    FloorFixture ff = (FloorFixture)fixtures.FirstOrDefault(fixture => fixture.name == fixtureName);

                    tileDefinition.fixtures.SetFloorFixtureAtLayer(ff, layer);
                    if (ff == null)
                    {
                        Debug.LogError($"Network recieved fixture with name {fixtureName} could not be found");
                    }
                }
            }

            // If the boolean is false, subStates should be null.
            if (reader.ReadBoolean())
            {
                using (var stream = new MemoryStream(reader.ReadBytesAndSize()))
                {
                    tileDefinition.subStates = new BinaryFormatter().Deserialize(stream) as object[];
                }
            }

            // TODO: Should substates be initialized to null array?
            return tileDefinition;
        }

        

        // Store a list of all turfs and fixtures to be used in networking communications.
        // This might not be the final place of these resources (could be a public singleton), given that these could be
        // used for other purposes, e.g. in-game tile editing, recipes, etc.
        private static Plenum[] plenums = Resources.FindObjectsOfTypeAll<Plenum>();
        private static Turf[] turfs = Resources.FindObjectsOfTypeAll<Turf>();
        private static Fixture[] fixtures = Resources.FindObjectsOfTypeAll<Fixture>();
    }
}