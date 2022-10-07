using System;
using System.Collections.Generic;
using FishNet;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Tile
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
        /// <param name="tileObjectSo"></param>
        /// <returns></returns>
        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, Direction dir, TileObjectSo tileObjectSo)
        {

            GameObject placedGameObject = EditorAndRuntime.InstantiatePrefab(tileObjectSo.prefab);
            placedGameObject.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            // Alternative name is required for walls as they can occupy the same tile
            if (TileHelper.ContainsSubLayers(tileObjectSo.layer))
                placedGameObject.name += "_" + TileHelper.GetDirectionIndex(dir);

            PlacedTileObject placedObject = placedGameObject.GetComponent<PlacedTileObject>();
            if (placedObject == null)
            {
                placedObject = placedGameObject.AddComponent<PlacedTileObject>();
            }

            placedObject.Setup(tileObjectSo, origin, dir);

            if (InstanceFinder.ServerManager?.Started ?? false)
            {
                InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }

        private TileObjectSo _tileObjectSo;
        private Vector2Int _origin;
        private Direction _dir;
        private IAdjacencyConnector _adjacencyConnector;

        /// <summary>
        /// Set up a new PlacedTileObject.
        /// </summary>
        /// <param name="tileObjectSo"></param>
        /// <param name="origin"></param>
        /// <param name="dir"></param>
        public void Setup(TileObjectSo tileObjectSo, Vector2Int origin, Direction dir)
        {
            _tileObjectSo = tileObjectSo;
            _origin = origin;
            _dir = dir;
            _adjacencyConnector = GetComponent<IAdjacencyConnector>();
        }

        /// <summary>
        /// Returns a list of all grids positions that object occupies.
        /// </summary>
        /// <returns></returns>
        public List<Vector2Int> GetGridPositionList()
        {
            return _tileObjectSo.GetGridPositionList(_origin, _dir);
        }

        /// <summary>
        /// Destroys itself.
        /// </summary>
        public void DestroySelf()
        {
            if (_adjacencyConnector != null)
                _adjacencyConnector.CleanAdjacencies();
            EditorAndRuntime.Destroy(gameObject);
        }

        public override string ToString()
        {
            return _tileObjectSo.nameString;
        }

        /// <summary>
        /// Returns a new SaveObject for use in saving/loading.
        /// </summary>
        /// <returns></returns>
        public PlacedSaveObject Save()
        {
            return new PlacedSaveObject
            {
                tileObjectSOName = _tileObjectSo.nameString,
                origin = _origin,
                dir = _dir,
            };
        }

        /// <summary>
        /// Returns if an adjacency connector is present.
        /// </summary>
        /// <returns></returns>
        public bool HasAdjacencyConnector()
        {
            return _adjacencyConnector != null;
        }

        /// <summary>
        /// Sends an update to the adjacency connector for all neighbouring objects.
        /// </summary>
        /// <param name="placedObjects"></param>
        public void UpdateAllAdjacencies(PlacedTileObject[] placedObjects)
        {
            if (_adjacencyConnector != null)
                _adjacencyConnector.UpdateAll(placedObjects);
        }

        /// <summary>
        /// Sends an update to the adjacency connector one neigbouring object.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="placedNeighbour"></param>
        public void UpdateSingleAdjacency(Direction dir, PlacedTileObject placedNeighbour)
        {
            if (_adjacencyConnector != null)
                _adjacencyConnector.UpdateSingle(dir, placedNeighbour);
        }

        public TileObjectGenericType GetGenericType()
        {
            if (_tileObjectSo != null)
            {
                return _tileObjectSo.genericType;
            }

            return TileObjectGenericType.None;
        }

        public TileObjectSpecificType GetSpecificType()
        {
            if (_tileObjectSo != null)
            {
                return _tileObjectSo.specificType;
            }

            return TileObjectSpecificType.None;
        }

        public string GetName()
        {
            if (_tileObjectSo != null)
                return _tileObjectSo.nameString;
            else
                return string.Empty;
        }

        public Direction GetDirection()
        {
            return _dir;
        }

        public TileLayer GetLayer()
        {
            return _tileObjectSo.layer;
        }
    }
}