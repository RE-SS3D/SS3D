using FishNet;
using FishNet.Object;
using SS3D.Systems.Tile.Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public class PlacedTileObject: NetworkBehaviour
    {
        /// <summary>
        /// Creates a new PlacedTileObject from a TileObjectSO at a given position and direction. Uses NetworkServer.Spawn() if a server is running.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="dir"></param>
        /// <param name="tileObjectSo"></param>
        /// <returns></returns>
        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, Direction dir, TileObjectSo tileObjectSo)
        {
            GameObject placedGameObject = Instantiate(tileObjectSo.prefab);
            placedGameObject.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            PlacedTileObject placedObject = placedGameObject.GetComponent<PlacedTileObject>();
            if (placedObject == null)
            {
                placedObject = placedGameObject.AddComponent<PlacedTileObject>();
            }

            placedObject.Setup(tileObjectSo, origin, dir);

            if (InstanceFinder.ServerManager != null && placedObject.GetComponent<NetworkObject>() != null)
            {
                InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }



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

        private TileObjectSo _tileObjectSo;
        private Vector2Int _origin;
        private Direction _dir;
        private IAdjacencyConnector _connector;

        /// <summary>
        /// Set up a new PlacedTileObject.
        /// </summary>
        /// <param name="tileObjectSo"></param>
        /// <param name="dir"></param>
        private void Setup(TileObjectSo tileObjectSo, Vector2Int origin, Direction dir)
        {
            _tileObjectSo = tileObjectSo;
            _origin = origin;
            _dir = dir;
            _connector = GetComponent<IAdjacencyConnector>();
        }

        /// <summary>
        /// Destroys itself.
        /// </summary>
        public void DestroySelf()
        {
            base.Despawn();
        }

        /// <summary>
        /// Returns a list of all grids positions that object occupies.
        /// </summary>
        /// <returns></returns>
        public List<Vector2Int> GetGridOffsetList()
        {
            return _tileObjectSo.GetGridOffsetList(_dir);
        }

        public Vector2Int GetOrigin()
        {
            return _origin;
        }

        public TileObjectGenericType GetGenericType()
        {
            return _tileObjectSo.genericType;
        }

        public TileObjectSpecificType GetSpecificType()
        {
            return _tileObjectSo.specificType;
        }

        public string GetNameString()
        {
            return _tileObjectSo.nameString;
        }

        public bool HasAdjacencyConnector()
        {
            return _connector != null;
        }

        public void UpdateAdjacencies(PlacedTileObject[] neighbourObjects)
        {
            if (HasAdjacencyConnector())
                _connector.UpdateAll(neighbourObjects);
        }

        public void UpdateSingleAdjacency(PlacedTileObject neighbourObject, Direction dir)
        {
            if (HasAdjacencyConnector())
                _connector.UpdateSingle(dir, neighbourObject, false);
        }
    }
}