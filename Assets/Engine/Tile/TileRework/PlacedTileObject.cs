using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using SS3D.Engine.TilesRework.Connections;
using UnityEditor;

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
            // GameObject placedGameObject = Instantiate(tileObjectSO.prefab, worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            GameObject placedGameObject = EditorAndRuntime.InstantiatePrefab(tileObjectSO.prefab);

            // Undo.RecordObject(placedGameObject.transform, "Change rotation");
            // placedGameObject.transform.position = worldPosition;
            // placedGameObject.transform.rotation = Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0);
            placedGameObject.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));


            // Alternative name is required for walls as they can occupy the same tile
            if (tileObjectSO.layerType == TileLayer.HighWall || tileObjectSO.layerType == TileLayer.LowWall)
                placedGameObject.name += "_" + TileHelper.GetDirectionIndex(dir);


            // EditorUtility.SetDirty(placedGameObject.transform);
            // PrefabUtility.RecordPrefabInstancePropertyModifications(placedGameObject.transform);

            /*
            var so = new SerializedObject(placedGameObject.transform);
            so.FindProperty("m_LocalRotation").quaternionValue = Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0);
            so.ApplyModifiedProperties();
            */

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

        public void Setup(TileObjectSO tileObjectSO, Vector2Int origin, Direction dir)
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

        [ContextMenu("Force rotate")]
        private void ForceRotate()
        {
            transform.rotation = Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0);
        }
    }
}