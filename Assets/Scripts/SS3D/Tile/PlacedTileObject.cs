using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Server;
using FishNet.Managing.Client;
using SS3D.Engine.Tiles;
using SS3D.Engine.Tiles.Connections;
using UnityEngine;

namespace SS3D.Engine.Tile.TileRework
{
    /// <summary>
    /// Class that is attached to every GameObject placed on the TileMap. 
    /// </summary>
    public class PlacedTileObject : MonoBehaviour
    {
        /// <summary>
        /// SaveObject that contains all information required to reconstruct the object.
        /// </summary>
        [Serializable]
        public class PlacedSaveObject
        {
            public string tileObjectSOName;
            public Vector2Int origin;
            public Direction dir;
        }

        /// <summary>
        /// Creates a new PlacedTileObject from a TileObjectSO at a given position and direction. Uses NetworkServer.Spawn() if a server is running.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="origin"></param>
        /// <param name="dir"></param>
        /// <param name="tileObjectSO"></param>
        /// <returns></returns>
        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, Direction dir, TileObjectSo tileObjectSO)
        {

            GameObject placedGameObject = EditorAndRuntime.InstantiatePrefab(tileObjectSO.prefab);
            placedGameObject.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            // Alternative name is required for walls as they can occupy the same tile
            if (TileHelper.ContainsSubLayers(tileObjectSO.layer))
                placedGameObject.name += "_" + TileHelper.GetDirectionIndex(dir);

            PlacedTileObject placedObject = placedGameObject.GetComponent<PlacedTileObject>();
            if (placedObject == null)
            {
                placedObject = placedGameObject.AddComponent<PlacedTileObject>();
            }

            placedObject.Setup(tileObjectSO, origin, dir);

            if (InstanceFinder.ServerManager?.Started ?? false)
            {
                InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }

        private TileObjectSo tileObjectSO;
        private Vector2Int origin;
        private Direction dir;
        private IAdjacencyConnector adjacencyConnector;

        /// <summary>
        /// Set up a new PlacedTileObject.
        /// </summary>
        /// <param name="tileObjectSO"></param>
        /// <param name="origin"></param>
        /// <param name="dir"></param>
        public void Setup(TileObjectSo tileObjectSO, Vector2Int origin, Direction dir)
        {
            this.tileObjectSO = tileObjectSO;
            this.origin = origin;
            this.dir = dir;
            adjacencyConnector = GetComponent<IAdjacencyConnector>();
        }

        /// <summary>
        /// Returns a list of all grids positions that object occupies.
        /// </summary>
        /// <returns></returns>
        public List<Vector2Int> GetGridPositionList()
        {
            return tileObjectSO.GetGridPositionList(origin, dir);
        }

        /// <summary>
        /// Destroys itself.
        /// </summary>
        public void DestroySelf()
        {
            if (adjacencyConnector != null)
                adjacencyConnector.CleanAdjacencies();
            EditorAndRuntime.Destroy(gameObject);
        }

        public override string ToString()
        {
            return tileObjectSO.nameString;
        }

        /// <summary>
        /// Returns a new SaveObject for use in saving/loading.
        /// </summary>
        /// <returns></returns>
        public PlacedSaveObject Save()
        {
            return new PlacedSaveObject
            {
                tileObjectSOName = tileObjectSO.nameString,
                origin = origin,
                dir = dir,
            };
        }

        /// <summary>
        /// Returns if an adjacency connector is present.
        /// </summary>
        /// <returns></returns>
        public bool HasAdjacencyConnector()
        {
            return adjacencyConnector != null;
        }

        /// <summary>
        /// Sends an update to the adjacency connector for all neighbouring objects.
        /// </summary>
        /// <param name="placedObjects"></param>
        public void UpdateAllAdjacencies(PlacedTileObject[] placedObjects)
        {
            if (adjacencyConnector != null)
                adjacencyConnector.UpdateAll(placedObjects);
        }

        /// <summary>
        /// Sends an update to the adjacency connector one neigbouring object.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="placedNeighbour"></param>
        public void UpdateSingleAdjacency(Direction dir, PlacedTileObject placedNeighbour)
        {
            if (adjacencyConnector != null)
                adjacencyConnector.UpdateSingle(dir, placedNeighbour);
        }

        public TileObjectGenericType GetGenericType()
        {
            if (tileObjectSO != null)
            {
                return tileObjectSO.genericType;
            }

            return TileObjectGenericType.None;
        }

        public TileObjectSpecificType GetSpecificType()
        {
            if (tileObjectSO != null)
            {
                return tileObjectSO.specificType;
            }

            return TileObjectSpecificType.None;
        }

        public string GetName()
        {
            if (tileObjectSO != null)
                return tileObjectSO.nameString;
            else
                return string.Empty;
        }

        public Direction GetDirection()
        {
            return dir;
        }

        public TileLayer GetLayer()
        {
            return tileObjectSO.layer;
        }
    }
}