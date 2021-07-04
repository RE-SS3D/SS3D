using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using SS3D.Engine.TilesRework.Connections;

namespace SS3D.Engine.TilesRework
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class PlacedTileObject : MonoBehaviour
    {
        [Serializable]
        public class PlacedSaveObject
        {
            public string tileObjectSOName;
            public Vector2Int origin;
            public Direction dir;
        }

        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, Direction dir, TileObjectSO tileObjectSO)
        {
            GameObject placedGameObject = Instantiate(tileObjectSO.prefab, worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            PlacedTileObject placedObject = placedGameObject.GetComponent<PlacedTileObject>();
            if (placedObject == null)
            {
                placedObject = placedGameObject.AddComponent<PlacedTileObject>();
            }

            placedObject.Setup(tileObjectSO, origin, dir);

            return placedObject;
        }

        private TileObjectSO tileObjectSO;
        private Vector2Int origin;
        private Direction dir;
        private IAdjacencyConnector adjacencyConnector;

        private void Setup(TileObjectSO tileObjectSO, Vector2Int origin, Direction dir)
        {
            this.tileObjectSO = tileObjectSO;
            this.origin = origin;
            this.dir = dir;
            adjacencyConnector = GetComponent<IAdjacencyConnector>();
        }

        public List<Vector2Int> GetGridPositionList()
        {
            return tileObjectSO.GetGridPositionList(origin, dir);
        }

        public void DestroySelf()
        {
            EditorAndRuntime.Destroy(gameObject);
        }

        public override string ToString()
        {
            return tileObjectSO.nameString;
        }

        public PlacedSaveObject Save()
        {
            return new PlacedSaveObject
            {
                tileObjectSOName = tileObjectSO.nameString,
                origin = origin,
                dir = dir,
            };
        }

        public bool HasAdjacencyConnector()
        {
            return adjacencyConnector != null;
        }

        public void UpdateAllAdjacencies(PlacedTileObject[] placedObjects)
        {
            adjacencyConnector?.UpdateAll(placedObjects);
        }

        public void UpdateSingleAdjacency(Direction dir, PlacedTileObject placedNeighbour)
        {
            adjacencyConnector?.UpdateSingle(dir, placedNeighbour);
        }
    }
}