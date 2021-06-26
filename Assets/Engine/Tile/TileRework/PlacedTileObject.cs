using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class PlacedTileObject : MonoBehaviour
    {
        [Serializable]
        public class SaveObject
        {
            public string tileObjectSOName;
            public Vector2Int origin;
            public TileObjectSO.Dir dir;
        }

        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, TileObjectSO.Dir dir, TileObjectSO tileObjectSO)
        {
            GameObject placedGameObject = Instantiate(tileObjectSO.prefab, worldPosition, Quaternion.Euler(0, TileObjectSO.GetRotationAngle(dir), 0));

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
        private TileObjectSO.Dir dir;

        private void Setup(TileObjectSO tileObjectSO, Vector2Int origin, TileObjectSO.Dir dir)
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
#if UNITY_EDITOR
            DestroyImmediate(gameObject);
#else
            Destroy(gameObject);
#endif
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