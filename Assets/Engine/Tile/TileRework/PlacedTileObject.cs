using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace SS3D.Engine.TilesRework
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class PlacedTileObject : MonoBehaviour
    {
        [Serializable]
        public class SaveObject
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

        private void Setup(TileObjectSO tileObjectSO, Vector2Int origin, Direction dir)
        {
            this.tileObjectSO = tileObjectSO;
            this.origin = origin;
            this.dir = dir;
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

        public SaveObject Save()
        {
            return new SaveObject
            {
                tileObjectSOName = tileObjectSO.nameString,
                origin = origin,
                dir = dir,
            };
        }
    }
}